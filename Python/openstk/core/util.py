import os, asyncio, time, yaml

def _throw(message: str) -> None: raise Exception(message)

def _pathExtension(path: str) -> str: return os.path.splitext(path)[1]

def _pathTempFile(ext: str) -> str:
    c = 0
    tmp_file = f'tmp/{c}.{ext}'
    if not os.path.exists('tmp'): os.mkdir('tmp')
    while os.path.exists(tmp_file):
        c += 1
        tmp_file = f'tmp/{c}.{ext}'
    return tmp_file

def decodePath(ApplicationPath: str, path: str, rootPath: str = None) -> str:
    lowerPath = path.lower()
    return f'{os.path.expanduser('~')}{path[1:]}' if lowerPath.startswith('~') else \
        f'{rootPath}{path[6:]}' if lowerPath.startswith('%path%') else \
        f'{ApplicationPath}{path[9:]}' if lowerPath.startswith('%apppath%') else \
        f'{os.getenv("APPDATA")}{path[9:]}' if lowerPath.startswith('%appdata%') else \
        f'{os.getenv("LOCALAPPDATA")}{path[14:]}' if lowerPath.startswith('%localappdata%') else \
        path

def _int_tryParse(s: str) -> int | None:
    try: return int(s)
    except ValueError: return None

#YamlDict
class YamlDict(dict):
    def __init__(self, file: str):
        self.path = decodePath(None, file)
        if not os.path.isfile(self.path): return
        try:
            with open(self.path, 'r', encoding='utf-8') as f:
                for k, v in yaml.safe_load(f).items(): self[k] = v
        except FileNotFoundError: print(f'Error: The file "{self.path}" was not found.')
        except UnicodeDecodeError: print(f'Error: Could not decode the file "{self.path}" with UTF-8 encoding.')
        except yaml.YAMLError as e: print(f'YAML Error: {e}')

    def flush(self):
        try:
            with open(self.path, 'w', encoding='utf-8') as f:
                yaml.dump(self, f, default_flow_style=False)
        except IOError: print(f'Error: Could not write to "{self.path}".')
        except yaml.YAMLError as e: print(f'YAML Error: {e}')

# Stopwatch
class Stopwatch:
    def __init__(self):
        self._start_time = None
        self._elapsed_time = 0
        self._running = False

    def start(self):
        if not self._running:
            self._start_time = time.time()
            self._running = True
            print("Stopwatch started.")
        else:
            print("Stopwatch is already running.")

    def stop(self):
        if self._running:
            self._elapsed_time += time.time() - self._start_time
            self._running = False
            print("Stopwatch stopped.")
        else:
            print("Stopwatch is not running.")

    def reset(self):
        self._start_time = None
        self._elapsed_time = 0
        self._running = False
        print("Stopwatch reset.")

    def get_elapsed_time(self):
        if self._running:
            return self._elapsed_time + (time.time() - self._start_time)
        return self._elapsed_time

    def display_time(self):
        total_seconds = int(self.get_elapsed_time())
        hours = total_seconds // 3600
        minutes = (total_seconds % 3600) // 60
        seconds = total_seconds % 60
        print(f"Elapsed Time: {hours:02d}:{minutes:02d}:{seconds:02d}")

