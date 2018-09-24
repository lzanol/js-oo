#include <cstdint>

class SamplesManager {
public:
    void render(float *buffer, int32_t channelStride, int32_t numFrames);
    void render(int16_t *buffer, int32_t channelStride, int32_t numFrames);
};