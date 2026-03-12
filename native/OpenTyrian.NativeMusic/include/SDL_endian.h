#ifndef SDL_ENDIAN_H
#define SDL_ENDIAN_H

#include "SDL_types.h"

#define SDL_LIL_ENDIAN 1234
#define SDL_BIG_ENDIAN 4321
#define SDL_BYTEORDER SDL_LIL_ENDIAN

static __inline Uint16 SDL_Swap16(Uint16 x)
{
    return (Uint16)((x << 8) | (x >> 8));
}

static __inline Uint32 SDL_Swap32(Uint32 x)
{
    return ((x & 0x000000FFu) << 24) |
           ((x & 0x0000FF00u) << 8) |
           ((x & 0x00FF0000u) >> 8) |
           ((x & 0xFF000000u) >> 24);
}

#endif
