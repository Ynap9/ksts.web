import { Injectable } from '@angular/core';

/** Below this alpha a pixel is the transparent background of a cut-out signature, not ink. */
const INK_ALPHA_MIN = 128;

/** Brighter than this (0..1) is the paper behind the signature — scans are never pure white. */
const INK_PAPER_LEVEL = 0.9;

/** Share of the darkest remaining pixels averaged into the answer: the core of the stroke, not its edges. */
const INK_DARKEST_SHARE = 0.1;

/** Longest edge the image is drawn at before pixels are read; a signature needs no more to show its colour. */
const INK_SAMPLE_MAX_PX = 600;

const LEVELS = 256;

/**
 * Reads the ink colour out of a wet-ink signature image so the colour picker can start from the colour the
 * user actually sees, instead of a black placeholder that lies about a blue signature.
 */
@Injectable({
    providedIn: 'root'
})
export class InkColorService {
    /** Resolves to "#RRGGBB", or null when the image cannot be read — the caller keeps its own fallback. */
    async extract(url: string): Promise<string | null> {
        const image = await this.loadImage(url);
        if (!image) {
            return null;
        }

        const pixels = this.readPixels(image);
        return pixels ? this.pickInkColor(pixels) : null;
    }

    loadImage(url: string): Promise<HTMLImageElement | null> {
        return new Promise((resolve) => {
            const image = new Image();
            // Images served from the object store are cross-origin: without this the canvas is tainted and
            // reading pixels throws. A blob URL of a file just picked from disk ignores the attribute.
            image.crossOrigin = 'anonymous';
            image.onload = () => resolve(image);
            image.onerror = () => resolve(null);
            image.src = url;
        });
    }

    /** Null when the browser refuses the pixels — an object store answering without CORS headers taints the canvas. */
    readPixels(image: HTMLImageElement): Uint8ClampedArray | null {
        const scale = Math.min(1, INK_SAMPLE_MAX_PX / Math.max(image.naturalWidth, image.naturalHeight, 1));
        const width = Math.max(1, Math.round(image.naturalWidth * scale));
        const height = Math.max(1, Math.round(image.naturalHeight * scale));

        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;

        const context = canvas.getContext('2d');
        if (!context) {
            return null;
        }

        context.drawImage(image, 0, 0, width, height);
        try {
            return context.getImageData(0, 0, width, height).data;
        } catch {
            return null;
        }
    }

    /**
     * Averages only the darkest slice of the ink. Averaging every non-paper pixel drags the answer towards
     * the paper and reports washed-out grey for a blue signature, because the anti-aliased edge of a stroke
     * holds far more pixels than its core.
     */
    pickInkColor(data: Uint8ClampedArray): string | null {
        const paperLevel = INK_PAPER_LEVEL * (LEVELS - 1);
        const histogram = new Array<number>(LEVELS).fill(0);
        let counted = 0;

        for (let i = 0; i < data.length; i += 4) {
            if (data[i + 3] < INK_ALPHA_MIN) {
                continue;
            }

            const level = this.inkLevel(data[i], data[i + 1], data[i + 2]);
            if (level > paperLevel) {
                continue;
            }

            histogram[level]++;
            counted++;
        }

        if (counted === 0) {
            return null;
        }

        const cutoff = this.darkestCutoff(histogram, Math.max(1, Math.round(counted * INK_DARKEST_SHARE)));
        let red = 0;
        let green = 0;
        let blue = 0;
        let taken = 0;

        for (let i = 0; i < data.length; i += 4) {
            if (data[i + 3] < INK_ALPHA_MIN || this.inkLevel(data[i], data[i + 1], data[i + 2]) > cutoff) {
                continue;
            }

            red += data[i];
            green += data[i + 1];
            blue += data[i + 2];
            taken++;
        }

        return taken === 0 ? null : this.toHex(red / taken, green / taken, blue / taken);
    }

    /** Brightness level 0..255 of a pixel, the same luma weights every image tool uses. */
    inkLevel(red: number, green: number, blue: number): number {
        return Math.round(0.299 * red + 0.587 * green + 0.114 * blue);
    }

    /** Brightness level the darkest `wanted` pixels fit under. */
    darkestCutoff(histogram: number[], wanted: number): number {
        let running = 0;
        for (let level = 0; level < histogram.length; level++) {
            running += histogram[level];
            if (running >= wanted) {
                return level;
            }
        }

        return histogram.length - 1;
    }

    toHex(red: number, green: number, blue: number): string {
        const channels = [red, green, blue].map((value) => Math.round(value).toString(16).padStart(2, '0')).join('');

        return `#${channels}`.toUpperCase();
    }
}
