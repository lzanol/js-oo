package com.lzanol.chords.audio

import android.content.Context
import android.media.AudioManager
import android.util.Log
import com.lzanol.chords.R
import java.nio.ByteBuffer
import java.nio.ByteOrder

object PlaybackEngine {

    private var mEngineHandle: Long = 0

    val currentOutputLatencyMillis: Double
        get() = if (mEngineHandle == 0L) 0.0 else nGetCurrentOutputLatencyMillis(mEngineHandle)

    val isLatencyDetectionSupported: Boolean
        get() = mEngineHandle != 0L && nIsLatencyDetectionSupported(mEngineHandle)

    // Load native library
    init {
        System.loadLibrary("audio-engine")
    }

    fun create(context: Context): Boolean {
        // instantiate native playback engine if it's not done yet
        if (mEngineHandle == 0L) {
            val myAudioMgr = context.getSystemService(Context.AUDIO_SERVICE) as AudioManager
            //val sampleRateStr = myAudioMgr.getProperty(AudioManager.PROPERTY_OUTPUT_SAMPLE_RATE)
            //val defaultSampleRate = Integer.parseInt(sampleRateStr)
            val framesPerBurstStr = myAudioMgr.getProperty(AudioManager.PROPERTY_OUTPUT_FRAMES_PER_BUFFER)
            val defaultFramesPerBurst = Integer.parseInt(framesPerBurstStr)

            //nSetDefaultSampleRate(defaultSampleRate)
            nSetDefaultSampleRate(48000)
            nSetDefaultFramesPerBurst(defaultFramesPerBurst)

            mEngineHandle = nCreateEngine()
        }

        return mEngineHandle != 0L
    }

    fun delete() {
        if (mEngineHandle != 0L)
            nDeleteEngine(mEngineHandle)

        mEngineHandle = 0
    }

    fun initialize(context: Context): Boolean {
        if (!create(context))
            return false

        // assuming: 44.1 kHz, 16-bit (short), 2 channels
        val shortFloatFactor = 1f / Short.MAX_VALUE
        val threeSecsInSamples = 48000 * 3 * 2
        val threeSecsInBytes = threeSecsInSamples * 4
        val samples = FloatArray(threeSecsInSamples)
        val samplesSecsInBytes = IntArray(1) { threeSecsInBytes }

        val inputStream = context.resources.openRawResource(R.raw.piano)
        val bytes = ByteArray(threeSecsInBytes)
        var i = 0

        inputStream.read(bytes, 44, threeSecsInBytes - 44)

        val buffer = ByteBuffer.wrap(bytes)
        buffer.order(ByteOrder.LITTLE_ENDIAN)

        while (i < threeSecsInSamples) {
            //samples[i] = Math.max(Math.min((bytes[j].toInt() shl 8 or bytes[j + 1].toInt()) * shortToFloatFactor, Float.MAX_VALUE), Float.MIN_VALUE)
            samples[i] = buffer.getShort(i * 2) * shortFloatFactor

            /*if (i < 100)
                Log.i("samples", String.format("%s %s %s", sample, shortFloatFactor, samples[i]))*/

            i++
        }

        return nInitialize(mEngineHandle, samples, samplesSecsInBytes)
    }

    fun setToneOn(isToneOn: Boolean) {
        nSetToneOn(mEngineHandle, isToneOn)
    }

    fun setAudioApi(audioApi: Int) {
        if (mEngineHandle != 0L) nSetAudioApi(mEngineHandle, audioApi)
    }

    fun setAudioDeviceId(deviceId: Int) {
        if (mEngineHandle != 0L) nSetAudioDeviceId(mEngineHandle, deviceId)
    }

    fun setChannelCount(channelCount: Int) {
        if (mEngineHandle != 0L) nSetChannelCount(mEngineHandle, channelCount)
    }

    fun setBufferSizeInBursts(bufferSizeInBursts: Int) {
        if (mEngineHandle != 0L) nSetBufferSizeInBursts(mEngineHandle, bufferSizeInBursts)
    }

    // Native methods
    private external fun nCreateEngine(): Long

    private external fun nDeleteEngine(engineHandle: Long)
    private external fun nInitialize(engineHandle: Long, samples: FloatArray, samplesSecsInBytes: IntArray): Boolean
    private external fun nSetToneOn(engineHandle: Long, isToneOn: Boolean)
    private external fun nSetAudioApi(engineHandle: Long, audioApi: Int)
    private external fun nSetAudioDeviceId(engineHandle: Long, deviceId: Int)
    private external fun nSetChannelCount(mEngineHandle: Long, channelCount: Int)
    private external fun nSetBufferSizeInBursts(engineHandle: Long, bufferSizeInBursts: Int)
    private external fun nGetCurrentOutputLatencyMillis(engineHandle: Long): Double
    private external fun nIsLatencyDetectionSupported(engineHandle: Long): Boolean
    private external fun nSetDefaultSampleRate(sampleRate: Int)
    private external fun nSetDefaultFramesPerBurst(framesPerBurst: Int)
}