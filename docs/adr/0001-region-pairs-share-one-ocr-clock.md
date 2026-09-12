# Region pairs share one OCR clock

Live overlay keeps a list of region pairs (each a capture region plus its own display region), not a primary slot plus a one-shot Region2 fallback probe. Every pair with a valid capture region runs at once. Sampling is one clock and per-pair small captures — not a full-screen grab, and not a timer per pair. OCR is a single serial queue. Settings may add at most four pairs for now; the engine ignores the ninth and later so a hand-edited config cannot unbounded-fork the hot path.
