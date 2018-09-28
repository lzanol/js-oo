package com.lzanol.chords.audio

import android.os.AsyncTask
import java.io.InputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

class SoundBankReader(val openResource: (id: Int) -> InputStream,
                      val callback: (notes: Array<FloatArray>?) -> Unit) :
        AsyncTask<Int, Void, Array<FloatArray>>() {

    override fun doInBackground(vararg params: Int?): Array<FloatArray> {
        // TODO (generalize): assuming 44.1 kHz, 16-bit (short), 2 channels
        val shortFloatFactor = 1f / Short.MAX_VALUE
        val threeSecsInSamples = 44100 * 3 * 2
        val threeSecsInBytes = threeSecsInSamples * 4
        val notes = ArrayList<FloatArray>()

        for (resourceId in params) {
            if (resourceId == null)
                continue

            val samples = FloatArray(threeSecsInSamples)

            openResource(resourceId).use {
                val bytes = ByteArray(threeSecsInBytes)
                var i = 0

                it.read(bytes)

                val buffer = ByteBuffer.wrap(bytes)

                buffer.order(ByteOrder.LITTLE_ENDIAN)

                // converting samples to float
                while (i < threeSecsInSamples) {
                    //samples[i] = Math.max(Math.min((bytes[j].toInt() shl 8 or bytes[j + 1].toInt()) * shortToFloatFactor, Float.MAX_VALUE), Float.MIN_VALUE)
                    samples[i] = buffer.getShort(i * 2) * shortFloatFactor
                    i++
                }

                notes.add(samples)
            }
        }

        return notes.toTypedArray()
    }

    override fun onPostExecute(result: Array<FloatArray>?) {
        super.onPostExecute(result)
        callback(result)
    }
}