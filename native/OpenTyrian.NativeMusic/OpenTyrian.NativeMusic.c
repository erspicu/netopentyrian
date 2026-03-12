#include "lds_play.h"
#include "opl.h"

#include <math.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define LDS_UPDATE2_RATE 139
#define VOLUME_RANGE 30.0f
#define TO_FIXED(x) ((int32_t)((x) * (1 << 12)))
#define FIXED_TO_INT(x) ((int32_t)((x) >> 12))

#if defined(_WIN32)
#define OTNM_EXPORT __declspec(dllexport)
#else
#define OTNM_EXPORT
#endif

int audioSampleRate = 0;

static FILE *g_musicFile = NULL;
static uint32_t *g_songOffsets = NULL;
static uint16_t g_songCount = 0;
static unsigned int g_songPlaying = 0;
static int g_musicStopped = 1;
static int g_samplesPerLdsUpdate = 0;
static int g_samplesPerLdsUpdateFrac = 0;
static int g_samplesUntilLdsUpdate = 0;
static int g_samplesUntilLdsUpdateFrac = 0;
static int32_t g_volumeFactorTable[256];
static int g_readFailed = 0;

OTNM_EXPORT int OpenTyrianMusic_Initialize(const char *musicFilePath, int sampleRate);
OTNM_EXPORT void OpenTyrianMusic_Shutdown(void);
OTNM_EXPORT int OpenTyrianMusic_PlaySong(int songIndex);
OTNM_EXPORT void OpenTyrianMusic_Stop(void);
OTNM_EXPORT int OpenTyrianMusic_Render(int16_t *output, int frameCount);

void fread_die(void *buffer, size_t size, size_t count, FILE *stream)
{
    size_t result = fread(buffer, size, count, stream);
    if (result != count)
    {
        size_t totalBytes = size * count;
        size_t readBytes = size * result;
        if (readBytes < totalBytes)
        {
            memset((unsigned char *)buffer + readBytes, 0, totalBytes - readBytes);
        }

        g_readFailed = 1;
    }
}

static void clear_state(void)
{
    g_songPlaying = 0;
    g_musicStopped = 1;
    g_samplesPerLdsUpdate = 0;
    g_samplesPerLdsUpdateFrac = 0;
    g_samplesUntilLdsUpdate = 0;
    g_samplesUntilLdsUpdateFrac = 0;
}

static long get_file_size(FILE *file)
{
    long position = ftell(file);
    fseek(file, 0, SEEK_END);
    long length = ftell(file);
    fseek(file, position, SEEK_SET);
    return length;
}

static void reset_update_counters(void)
{
    g_samplesUntilLdsUpdate = 0;
    g_samplesUntilLdsUpdateFrac = 0;
}

static void zero_samples(int16_t *output, int sampleCount)
{
    if (output != NULL && sampleCount > 0)
    {
        memset(output, 0, (size_t)sampleCount * sizeof(int16_t));
    }
}

static int load_song(unsigned int songIndex)
{
    unsigned int songSize;

    if (g_musicFile == NULL || g_songOffsets == NULL || songIndex >= g_songCount)
    {
        return 0;
    }

    songSize = g_songOffsets[songIndex + 1] - g_songOffsets[songIndex];
    g_readFailed = 0;
    if (!lds_load(g_musicFile, g_songOffsets[songIndex], songSize) || g_readFailed)
    {
        return 0;
    }

    reset_update_counters();
    return 1;
}

static void render_music(int16_t *output, int sampleCount)
{
    int16_t *remaining = output;
    int remainingCount = sampleCount;

    while (remainingCount > 0)
    {
        if (g_samplesUntilLdsUpdate == 0)
        {
            lds_update();
            g_samplesUntilLdsUpdate += g_samplesPerLdsUpdate;
            g_samplesUntilLdsUpdateFrac += g_samplesPerLdsUpdateFrac;
            if (g_samplesUntilLdsUpdateFrac >= LDS_UPDATE2_RATE)
            {
                g_samplesUntilLdsUpdate += 1;
                g_samplesUntilLdsUpdateFrac -= LDS_UPDATE2_RATE;
            }
        }

        {
            int count = g_samplesUntilLdsUpdate < remainingCount ? g_samplesUntilLdsUpdate : remainingCount;
            opl_update(remaining, count);
            remaining += count;
            remainingCount -= count;
            g_samplesUntilLdsUpdate -= count;
        }
    }
}

OTNM_EXPORT int OpenTyrianMusic_Initialize(const char *musicFilePath, int sampleRate)
{
    long fileSize;

    OpenTyrianMusic_Shutdown();

    if (musicFilePath == NULL || sampleRate <= 0)
    {
        return 0;
    }

    g_musicFile = fopen(musicFilePath, "rb");
    if (g_musicFile == NULL)
    {
        return 0;
    }

    if (fread(&g_songCount, sizeof(g_songCount), 1, g_musicFile) != 1 || g_songCount == 0)
    {
        OpenTyrianMusic_Shutdown();
        return 0;
    }

    g_songOffsets = (uint32_t *)malloc((size_t)(g_songCount + 1) * sizeof(uint32_t));
    if (g_songOffsets == NULL)
    {
        OpenTyrianMusic_Shutdown();
        return 0;
    }

    if (fread(g_songOffsets, sizeof(uint32_t), g_songCount, g_musicFile) != g_songCount)
    {
        OpenTyrianMusic_Shutdown();
        return 0;
    }

    fileSize = get_file_size(g_musicFile);
    if (fileSize <= 0)
    {
        OpenTyrianMusic_Shutdown();
        return 0;
    }

    g_songOffsets[g_songCount] = (uint32_t)fileSize;
    audioSampleRate = sampleRate;
    g_samplesPerLdsUpdate = 2 * (audioSampleRate / LDS_UPDATE2_RATE);
    g_samplesPerLdsUpdateFrac = 2 * (audioSampleRate % LDS_UPDATE2_RATE);
    g_volumeFactorTable[0] = 0;

    {
        size_t i;
        for (i = 1; i < 256; ++i)
        {
            g_volumeFactorTable[i] = TO_FIXED(powf(10.0f, (255.0f - (float)i) * (-VOLUME_RANGE / (20.0f * 255.0f))));
        }
    }

    clear_state();
    opl_init();
    return 1;
}

OTNM_EXPORT void OpenTyrianMusic_Shutdown(void)
{
    if (g_musicFile != NULL)
    {
        fclose(g_musicFile);
        g_musicFile = NULL;
    }

    if (g_songOffsets != NULL)
    {
        free(g_songOffsets);
        g_songOffsets = NULL;
    }

    lds_free();
    g_songCount = 0;
    clear_state();
}

OTNM_EXPORT int OpenTyrianMusic_PlaySong(int songIndex)
{
    if (songIndex < 0)
    {
        return 0;
    }

    if ((unsigned int)songIndex != g_songPlaying || g_musicStopped)
    {
        if (!load_song((unsigned int)songIndex))
        {
            return 0;
        }

        g_songPlaying = (unsigned int)songIndex;
    }

    g_musicStopped = 0;
    return 1;
}

OTNM_EXPORT void OpenTyrianMusic_Stop(void)
{
    g_musicStopped = 1;
}

OTNM_EXPORT int OpenTyrianMusic_Render(int16_t *output, int frameCount)
{
    int i;
    int32_t volumeFactor;

    if (output == NULL || frameCount <= 0)
    {
        return 0;
    }

    if (g_musicFile == NULL || g_musicStopped)
    {
        zero_samples(output, frameCount);
        return frameCount;
    }

    render_music(output, frameCount);

    volumeFactor = g_volumeFactorTable[255];
    volumeFactor *= 2;

    for (i = 0; i < frameCount; ++i)
    {
        int32_t sample = FIXED_TO_INT((int32_t)output[i] * volumeFactor);
        if (sample > INT16_MAX)
        {
            sample = INT16_MAX;
        }
        else if (sample < INT16_MIN)
        {
            sample = INT16_MIN;
        }

        output[i] = (int16_t)sample;
    }

    return frameCount;
}




