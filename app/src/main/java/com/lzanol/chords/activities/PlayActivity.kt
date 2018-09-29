package com.lzanol.chords.activities

import android.os.Bundle
import android.support.v7.app.AppCompatActivity
import android.util.Log
import android.view.MotionEvent
import android.view.View
import com.lzanol.chords.R
import com.lzanol.chords.audio.PlaybackEngine
import kotlinx.android.synthetic.main.activity_play.*

class PlayActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_play)

        PlaybackEngine.initialize(this, R.raw.guitar, {
            noteIndex, totalNotes ->
            progressText.text = String.format("%d/%d", noteIndex + 1, totalNotes)
        }) {
            if (it != null) {
                progressBar.visibility = View.GONE
                progressText.visibility = View.GONE
            } else Log.e("main", "Failed to initialize audio engine")
        }

        // configurations
        //PlaybackEngine.setChannelCount(Int) // 1,2,3,4,5,6,7,8
        //PlaybackEngine.setBufferSizeInBursts(Int) // 0(auto),1,2,4,8
        //PlaybackEngine.setAudioDeviceId(Int) // AudioDeviceCallback.onAudioDevicesAdded(addedDevices) { addedDevices.id }
        //PlaybackEngine.setAudioApi(Int) // 0(auto),1("OpenSL ES"),2("AAudio")
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> PlaybackEngine.setToneOn(true)
            MotionEvent.ACTION_UP -> PlaybackEngine.setToneOn(false)
        }

        return super.onTouchEvent(event)
    }

    override fun onDestroy() {
        PlaybackEngine.delete()
        super.onDestroy()
    }
}
