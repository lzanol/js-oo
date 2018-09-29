package com.lzanol.chords.audio

import android.os.AsyncTask
import android.util.Log
import java.io.InputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

class PatchReader(val openResource: (id: Int) -> InputStream,
                  val callback: (patch: Patch?) -> Unit) :
        AsyncTask<Int, Void, Patch>() {

    override fun doInBackground(vararg params: Int?): Patch {
        val shortFloatFactor = 1f / Short.MAX_VALUE
        val notes = ArrayList<FloatArray>()

        for (resourceId in params) {
            if (resourceId == null)
                continue

            openResource(resourceId).use {
                val buffer = ByteBuffer.wrap(it.readBytes())
                val span = 10
                var offset = 0

                buffer.order(ByteOrder.LITTLE_ENDIAN)

                for (n in 0 until span) {
                    // TODO (generalize): assuming 44.1 kHz, 16-bit (short), 2 channels
                    val size = 44100 * 4 * 2 * 3
                    val samples = FloatArray(size)
                    var i = 0

                    // converting samples to float
                    while (i < size) {
                        samples[i] = buffer.getShort(i * 2) * shortFloatFactor
                        i++
                    }

                    notes.add(samples)
                    offset += size
                }

                /*if (isWave) {
                    val size = buffer.capacity()

                    samples = FloatArray(size)

                    // converting samples to float
                    while (i < size) {
                        //samples[i] = Math.max(Math.min((bytes[j].toInt() shl 8 or bytes[j + 1].toInt()) * shortToFloatFactor, Float.MAX_VALUE), Float.MIN_VALUE)
                        samples[i] = buffer.getShort(i * 2) * shortFloatFactor
                        i++
                    }

                    notes.add(samples)
                    sizes.add(size)
                }*/
            }
        }

        return Patch(notes.toTypedArray(), "Guitar", 60, 29)
    }

    override fun onPostExecute(result: Patch?) {
        super.onPostExecute(result)
        callback(result)
    }
}