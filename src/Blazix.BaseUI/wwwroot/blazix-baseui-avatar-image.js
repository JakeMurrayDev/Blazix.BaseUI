export function loadImage(src, referrerPolicy, crossOrigin) {
    return new Promise((resolve) => {
        if (!src) {
            resolve('error');
            return;
        }

        const image = new Image();

        image.onload = () => resolve('loaded');
        image.onerror = () => resolve('error');

        if (referrerPolicy) {
            image.referrerPolicy = referrerPolicy;
        }
        if (crossOrigin) {
            image.crossOrigin = crossOrigin;
        }

        image.src = src;
    });
}