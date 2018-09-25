package com.lzanol.chords.audio

import android.content.Context
import android.media.AudioManager

object PlaybackEngine {

    private var mEngineHandle: Long = 0

    val currentOutputLatencyMillis: Double
        get() = if (mEngineHandle == 0L) 0.0 else nGetCurrentOutputLatencyMillis(mEngineHandle)

    val isLatencyDetectionSupported: Boolean
        get() = mEngineHandle != 0L && nIsLatencyDetectionSupported(mEngineHandle)

    init {
        // loading native engine
        System.loadLibrary("audio-engine")
    }

    fun initialize(context: Context, sounds: Array<Sound>, callback: (success: Boolean) -> Unit) {
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

        if (mEngineHandle == 0L) {
            callback(false)
            return
        }

        // loading sounds
        // Activity can't be referenced in long running tasks to avoid memory leaking
        SoundBankManager({ context.resources.openRawResource(it) }) { samples ->
            if (samples != null)
                callback(nInitialize(mEngineHandle, samples, samples.map { it.size }.toIntArray()))
            else callback(false)
        }.execute(*sounds)
    }

    fun delete() {
        if (mEngineHandle != 0L)
            nDeleteEngine(mEngineHandle)

        mEngineHandle = 0
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
    private external fun nInitialize(engineHandle: Long, samples: Array<FloatArray>, sizes: IntArray): Boolean
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