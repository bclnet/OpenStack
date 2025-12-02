def assertx(condition: bool, message: str = None) -> None: assert(condition)
def info(message: str) -> None: print(message)
def warn(message: str) -> None: print(f'WARN: {message}')
def error(message: str) -> None: print(f'ERROR: {message}')
def trace(message: str) -> None: print(f'TRACE: {message}')

# LogFile
class LogFile:
    def __init__(self, directory: str, file: str):
        self.logStream = None
    def __str__(self): return self.logStream.name
    def close(self) -> None: self.logStream.close()
    def write(self, message: str) -> None:
        pass
    def writeAsync(self, message: str) -> None:
        pass