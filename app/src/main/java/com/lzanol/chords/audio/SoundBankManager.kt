package com.lzanol.chords.audio

import android.os.AsyncTask
import java.io.InputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

class SoundBankManager(val openResource: (id: Int) -> InputStream,
                       val callback: (samples: Array<FloatArray>?) -> Unit) :
        AsyncTask<Sound, Void, Array<FloatArray>>() {

    override fun doInBackground(vararg params: Sound?): Array<FloatArray> {
        // assuming: 44.1 kHz, 16-bit (short), 2 channels
        val shortFloatFactor = 1f / Short.MAX_VALUE
        val threeSecsInSamples = 48000 * 3 * 2
        val threeSecsInBytes = threeSecsInSamples * 4
        val bank = ArrayList<FloatArray>()

        for (sound in params) {
            if (sound == null)
                continue

            val samples = FloatArray(threeSecsInSamples)
            val inputStream = openResource(sound.resourceId)
            val bytes = ByteArray(threeSecsInBytes)
            var i = 0

            inputStream.read(bytes)

            val buffer = ByteBuffer.wrap(bytes)

            buffer.order(ByteOrder.LITTLE_ENDIAN)

            while (i < threeSecsInSamples) {
                //samples[i] = Math.max(Math.min((bytes[j].toInt() shl 8 or bytes[j + 1].toInt()) * shortToFloatFactor, Float.MAX_VALUE), Float.MIN_VALUE)
                samples[i] = buffer.getShort(i * 2) * shortFloatFactor

                /*if (i < 100)
                    Log.i("samples", String.format("%s %s %s", sample, shortFloatFactor, samples[i]))*/

                i++
            }

            bank.add(samples)
        }

        return bank.toTypedArray()
    }

    override fun onPostExecute(result: Array<FloatArray>?) {
        super.onPostExecute(result)
        callback(result)
    }
}