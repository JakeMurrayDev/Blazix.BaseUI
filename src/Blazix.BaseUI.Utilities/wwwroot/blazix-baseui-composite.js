export function stopEvent(event) {
    event.preventDefault();
    event.stopPropagation();
}

export function focusElement(element) {
    if (element) {
        element.focus();
    }
}

export function scrollIntoViewIfNeeded(container, element, direction, orientation) {
    if (!container || !element) {
        return;
    }

    const containerRect = container.getBoundingClientRect();
    const elementRect = element.getBoundingClientRect();

    const isHorizontal = orientation === 'horizontal' || orientation === 'both';
    const isVertical = orientation === 'vertical' || orientation === 'both';

    let targetX = container.scrollLeft;
    let targetY = container.scrollTop;

    if (isVertical) {
        if (elementRect.top < containerRect.top) {
            targetY = container.scrollTop + (elementRect.top - containerRect.top);
        } else if (elementRect.bottom > containerRect.bottom) {
            targetY = container.scrollTop + (elementRect.bottom - containerRect.bottom);
        }
    }

    if (isHorizontal) {
        if (direction === 'ltr') {
            if (elementRect.left < containerRect.left) {
                targetX = container.scrollLeft + (elementRect.left - containerRect.left);
            } else if (elementRect.right > containerRect.right) {
                targetX = container.scrollLeft + (elementRect.right - containerRect.right);
            }
        } else {
            if (elementRect.right > containerRect.right) {
                targetX = container.scrollLeft + (elementRect.right - containerRect.right);
            } else if (elementRect.left < containerRect.left) {
                targetX = container.scrollLeft + (elementRect.left - containerRect.left);
            }
        }
    }

    container.scrollTo({
        left: targetX,
        top: targetY,
        behavior: 'smooth'
    });
}

export function getElementIndex(element, container) {
    if (!element || !container) {
        return -1;
    }

    const children = Array.from(container.children);
    return children.indexOf(element);
}

export function sortElementsByDomPosition(elementIds) {
    const elements = elementIds
        .map(id => document.querySelector(`[data-composite-id="${id}"]`))
        .filter(el => el !== null);

    elements.sort((a, b) => {
        const position = a.compareDocumentPosition(b);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) {
            return -1;
        }
        if (position & Node.DOCUMENT_POSITION_PRECEDING) {
            return 1;
        }
        return 0;
    });

    return elements.map(el => el.getAttribute('data-composite-id'));
}