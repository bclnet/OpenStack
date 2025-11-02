import io
from unittest import TestCase, main
from poly import Reader

# TestReader
class TestReader(Reader, TestCase):
    test = io.BytesIO(b'some initial binary data: \x00\x01')
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(self.test)

    def test__init__(self):
        pass

if __name__ == "__main__":
    main(verbosity=1)