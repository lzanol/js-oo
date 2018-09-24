package com.lzanol.chords.activities

import android.media.AudioManager
import android.os.Build
import android.os.Bundle
import android.support.v4.view.MotionEventCompat
import android.support.v7.app.AppCompatActivity
import android.util.Log
import android.view.MotionEvent
import android.view.View
import android.widget.*
import com.google.sample.audio_device.AudioDeviceListEntry
import com.google.sample.audio_device.AudioDeviceSpinner
import com.lzanol.chords.R
import com.lzanol.chords.audio.PlaybackEngine
import kotlinx.coroutines.experimental.runBlocking
import java.util.*
import kotlin.collections.HashMap

class PlayActivity : AppCompatActivity() {

    private val TAG = PlayActivity::class.java.name
    private val UPDATE_LATENCY_EVERY_MILLIS: Long = 1000
    private val CHANNEL_COUNT_OPTIONS = arrayOf(1, 2, 3, 4, 5, 6, 7, 8)
    // Default to Stereo (OPTIONS is zero-based array so index 1 = 2 channels)
    private val CHANNEL_COUNT_DEFAULT_OPTION_INDEX = 1
    private val BUFFER_SIZE_OPTIONS = intArrayOf(0, 1, 2, 4, 8)
    private val AUDIO_API_OPTIONS = arrayOf("Unspecified", "OpenSL ES", "AAudio")

    private var mAudioApiSpinner: Spinner? = null
    private var mPlaybackDeviceSpinner: AudioDeviceSpinner? = null
    private var mChannelCountSpinner: Spinner? = null
    private var mBufferSizeSpinner: Spinner? = null
    private var mLatencyText: TextView? = null
    private var mLatencyUpdater: Timer? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_play)

        setupAudioApiSpinner()
        setupPlaybackDeviceSpinner()
        setupChannelCountSpinner()
        setupBufferSizeSpinner()

        // initialize native audio system
        //PlaybackEngine.create(this)

        val self = this

        runBlocking {
            if (!PlaybackEngine.initialize(self))
                Log.e("main", "Failed to initialize audio stream")
        }

        // Periodically update the UI with the output stream latency
        mLatencyText = findViewById(R.id.latencyText)
        setupLatencyUpdater()
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        val action = MotionEventCompat.getActionMasked(event)
        when (action) {
            MotionEvent.ACTION_DOWN -> PlaybackEngine.setToneOn(true)
            MotionEvent.ACTION_UP -> PlaybackEngine.setToneOn(false)
        }
        return super.onTouchEvent(event)
    }

    private fun setupChannelCountSpinner() {
        mChannelCountSpinner = findViewById(R.id.channelCountSpinner)

        if (mChannelCountSpinner == null)
            return

        val mChannelCountSpinner = mChannelCountSpinner!!
        val channelCountAdapter = ArrayAdapter(this, R.layout.channel_counts_spinner, CHANNEL_COUNT_OPTIONS)

        mChannelCountSpinner.adapter = channelCountAdapter
        mChannelCountSpinner.setSelection(CHANNEL_COUNT_DEFAULT_OPTION_INDEX)

        mChannelCountSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onNothingSelected(parent: AdapterView<*>?) {}

            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                PlaybackEngine.setChannelCount(CHANNEL_COUNT_OPTIONS[mChannelCountSpinner.selectedItemPosition])
            }

        }
    }

    private fun setupBufferSizeSpinner() {
        mBufferSizeSpinner = findViewById(R.id.bufferSizeSpinner)

        if (mBufferSizeSpinner == null)
            return

        val mBufferSizeSpinner = mBufferSizeSpinner!!

        mBufferSizeSpinner.adapter = SimpleAdapter(
                this,
                createBufferSizeOptionsList(), // list of buffer size options
                R.layout.buffer_sizes_spinner, // the xml layout
                arrayOf(getString(R.string.buffer_size_description_key)), // field to display
                intArrayOf(R.id.bufferSizeOption) // View to show field in
        )

        mBufferSizeSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(adapterView: AdapterView<*>?, view: View?, position: Int, id: Long) {
                PlaybackEngine.setBufferSizeInBursts(getBufferSizeInBursts())
            }

            override fun onNothingSelected(adapterView: AdapterView<*>?) {

            }
        }
    }

    private fun setupPlaybackDeviceSpinner() {
        mPlaybackDeviceSpinner = findViewById(R.id.playbackDevicesSpinner)

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            mPlaybackDeviceSpinner?.setDirectionType(AudioManager.GET_DEVICES_OUTPUTS)
            mPlaybackDeviceSpinner?.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onItemSelected(adapterView: AdapterView<*>?, view: View?, position: Int, id: Long) {
                    PlaybackEngine.setAudioDeviceId(getPlaybackDeviceId())
                }

                override fun onNothingSelected(adapterView: AdapterView<*>?) {

                }
            }
        }
    }

    private fun setupAudioApiSpinner() {
        mAudioApiSpinner = findViewById(R.id.audioApiSpinner)

        if (mAudioApiSpinner == null)
            return

        val mAudioApiSpinner = mAudioApiSpinner!!

        mAudioApiSpinner.adapter = SimpleAdapter(
                this,
                createAudioApisOptionsList(),
                R.layout.audio_apis_spinner, // the xml layout
                arrayOf(getString(R.string.audio_api_description_key)), // field to display
                intArrayOf(R.id.audioApiOption) // View to show field in
        )

        mAudioApiSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(adapterView: AdapterView<*>?, view: View?, position: Int, id: Long) {
                PlaybackEngine.setAudioApi(position)
            }

            override fun onNothingSelected(adapterView: AdapterView<*>?) {

            }
        }
    }

    private fun getPlaybackDeviceId(): Int {
        return (mPlaybackDeviceSpinner?.selectedItem as AudioDeviceListEntry).id
    }

    private fun getBufferSizeInBursts(): Int {
        val selectedOption = mBufferSizeSpinner?.selectedItem as HashMap<String, String>

        val valueKey = getString(R.string.buffer_size_value_key)

        // parseInt will throw a NumberFormatException if the string doesn't contain a valid integer
        // representation. We don't need to worry about this because the values are derived from
        // the BUFFER_SIZE_OPTIONS int array.
        return Integer.parseInt(selectedOption[valueKey])
    }

    private fun setupLatencyUpdater() {

        //Update the latency every 1s
        val latencyUpdateTask = object : TimerTask() {
            override fun run() {

                val latencyStr: String

                latencyStr = if (PlaybackEngine.isLatencyDetectionSupported) {
                    val latency = PlaybackEngine.currentOutputLatencyMillis

                    if (latency >= 0)
                        String.format(Locale.getDefault(), "%.2fms", latency)
                    else "Unknown"
                } else getString(R.string.only_supported_on_api_26)

                runOnUiThread { mLatencyText?.text = getString(R.string.latency, latencyStr) }
            }
        }

        mLatencyUpdater = Timer()
        mLatencyUpdater?.schedule(latencyUpdateTask, 0, UPDATE_LATENCY_EVERY_MILLIS)

    }

    override fun onDestroy() {
        if (mLatencyUpdater != null)
            mLatencyUpdater?.cancel()

        PlaybackEngine.delete()
        super.onDestroy()
    }

    /**
     * Creates a list of buffer size options which can be used to populate a SimpleAdapter.
     * Each option has a description and a value. The description is always equal to the value,
     * except when the value is zero as this indicates that the buffer size should be set
     * automatically by the audio engine
     *
     * @return list of buffer size options
     */
    private fun createBufferSizeOptionsList(): List<HashMap<String, String>> {

        val bufferSizeOptions = ArrayList<HashMap<String, String>>()

        for (i in BUFFER_SIZE_OPTIONS) {
            val option = HashMap<String, String>()
            val strValue = i.toString()
            val description = if (i == 0) getString(R.string.automatic) else strValue
            option[getString(R.string.buffer_size_description_key)] = description
            option[getString(R.string.buffer_size_value_key)] = strValue

            bufferSizeOptions.add(option)
        }

        return bufferSizeOptions
    }

    private fun createAudioApisOptionsList(): List<HashMap<String, String>> {

        val audioApiOptions = ArrayList<HashMap<String, String>>()

        for (i in 0 until AUDIO_API_OPTIONS.size) {
            val option = HashMap<String, String>()
            option[getString(R.string.buffer_size_description_key)] = AUDIO_API_OPTIONS[i]
            option[getString(R.string.buffer_size_value_key)] = i.toString()
            audioApiOptions.add(option)
        }
        return audioApiOptions
    }
}
