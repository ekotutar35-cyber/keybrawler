"""Group nearby OCR lines without joining text from distant bubbles."""


def group_lines(results, gap=12):
    lines = []
    for box, text, confidence in results or []:
        if confidence < 0.75 or not text.strip():
            continue
        xs, ys = zip(*box)
        lines.append([min(xs), min(ys), max(xs), max(ys), text.strip()])
    lines.sort(key=lambda item: (item[1], item[0]))
    groups = []
    for left, top, right, bottom, text in lines:
        candidates = [g for g in groups if 0 <= top - g[3] <= gap
                      and min(right, g[2]) > max(left, g[0])]
        if candidates:
            g = min(candidates, key=lambda item: top - item[3])
            g[0], g[2], g[3] = min(left, g[0]), max(right, g[2]), bottom
            g[4] += ' ' + text
        else:
            groups.append([left, top, right, bottom, text])
    return groups
