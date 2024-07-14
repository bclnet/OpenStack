from unittest import TestCase, main
from PyQt6.QtWidgets import QApplication
from gl_view import OpenGLView

# TestOpenGlView
# @unittest.skipUnless(sys.platform.startswith("xwin"), "Requires Windows")
class TestOpenGlView(OpenGLView, TestCase): 
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        print(timer)
        #self.assertEqual(timer, 0)
        pass

    # def test_zero(self):
    #     self.assertEqual(abs(0), 0)

if __name__ == "__main__":
    app: QApplication = QApplication([])
    main(verbosity=2)

