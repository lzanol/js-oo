package com.lzanol.chords.activities

import android.graphics.Point
import android.os.Build
import android.os.Bundle
import android.support.v7.app.AppCompatActivity
import android.util.Log
import android.view.MotionEvent
import android.view.View
import com.lzanol.chords.R
import com.lzanol.chords.audio.PlaybackEngine
import kotlinx.android.synthetic.main.activity_play.*

class PlayActivity : AppCompatActivity() {

    private val windowMetrics = Point()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_play)

        PlaybackEngine.initialize(this, R.raw.piano, { noteIndex, totalNotes ->
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

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)

        if (hasFocus) {
            var visibility = View.SYSTEM_UI_FLAG_LAYOUT_STABLE or
                    View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION or
                    View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN or
                    View.SYSTEM_UI_FLAG_HIDE_NAVIGATION or
                    View.SYSTEM_UI_FLAG_FULLSCREEN

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT)
                visibility = visibility or View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY

            window.decorView.systemUiVisibility = visibility

            windowManager.defaultDisplay.getRealSize(windowMetrics)
        }
    }

    private val notes = intArrayOf(0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19)

    override fun onTouchEvent(event: MotionEvent): Boolean {
        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> PlaybackEngine.playNotes(intArrayOf(notes[Math.round(event.x / windowMetrics.x * 10)]))
            //MotionEvent.ACTION_UP -> PlaybackEngine.playNotes(intArrayOf())
        }

        return super.onTouchEvent(event)
    }

    override fun onDestroy() {
        PlaybackEngine.delete()
        super.onDestroy()
    }
}
