package com.lzanol.chords.audio

class Patch(
        val notes: Array<FloatArray>,
        val label: String,
        val span: Int,
        val lowest: Int,
        val octave: Int = 4,
        val transpose: Int = 0)
