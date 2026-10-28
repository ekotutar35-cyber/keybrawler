import unittest
from bubbles import group_lines


def line(x, y, text, score=0.99):
    return ([[x, y], [x+80, y], [x+80, y+20], [x, y+20]], text, score)


class GroupTests(unittest.TestCase):
    def test_separate_bubbles(self):
        groups = group_lines([line(0, 0, 'first'), line(200, 0, 'second')])
        self.assertEqual([g[4] for g in groups], ['first', 'second'])

    def test_multiline(self):
        groups = group_lines([line(0, 0, 'two'), line(0, 25, 'words'), line(0, 100, 'next')])
        self.assertEqual([g[4] for g in groups], ['two words', 'next'])

    def test_uncertain_text_ignored(self):
        self.assertEqual(group_lines([line(0, 0, 'noise', 0.4)]), [])

    def test_empty_frame(self):
        self.assertEqual(group_lines(None), [])
