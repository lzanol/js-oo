package com.lzanol.chords.audio

import android.os.AsyncTask
import java.io.InputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

class PatchReader(
        private val openResource: (id: Int) -> InputStream,
        private val onProgress: ((noteIndex: Int, totalNotes: Int) -> Unit)? = null,
        private val callback: ((patch: Patch?) -> Unit)? = null
) : AsyncTask<Int, Int, Patch>() {
    override fun doInBackground(vararg params: Int?): Patch {
        val shortFloatFactor = 1f / Short.MAX_VALUE
        val notes = ArrayList<FloatArray>()
        var span: Int

        for (resourceId in params) {
            if (resourceId == null)
                continue

            openResource(resourceId).use {
                val buffer = ByteBuffer.wrap(it.readBytes())
                var offset = 0

                span = 20

                buffer.order(ByteOrder.LITTLE_ENDIAN)

                for (n in 0 until span) {
                    // TODO (generalize): assuming 44.1 kHz, 16-bit (short), 2 channels
                    val totalSamples = 44100 * 2 * 3
                    val samples = FloatArray(totalSamples)
                    var i = 0

                    // converting samples to float
                    while (i < totalSamples) {
                        samples[i] = buffer.getShort(i * 2 + offset) * shortFloatFactor
                        i++
                    }

                    notes.add(samples)
                    offset += totalSamples
                    publishProgress(n, span)
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

        return Patch(notes.toTypedArray(), "Guitar", 10, 29)
    }

    override fun onProgressUpdate(vararg values: Int?) {
        super.onProgressUpdate(*values)
        onProgress?.invoke(values[0]!!, values[1]!!)
    }

    override fun onPostExecute(result: Patch?) {
        super.onPostExecute(result)
        callback?.invoke(result)
    }
}