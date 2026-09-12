# Voice plays only from the designated capture

Voice is one speaker among region pairs. The operator designates a voice-primary region in settings (default: the first region pair). Other pairs still update their own display regions but never start playback — including after the voice-primary pair has been cleared for stable no-text. Deleting the designated pair moves the designation to the first remaining pair. A hotkey was rejected: designation is a setup choice, not a combat toggle. Temporary follow was rejected because it would revive fallback-probe stealing for audio, so side text would speak in the gaps between dialogue lines.

Two exceptions sit outside the pair list: when voice playback is on, a dark-screen subtitle and a dialogue-choice echo may start playback. They do not change which pair is voice-primary, and they do not interrupt a line already playing from it.
