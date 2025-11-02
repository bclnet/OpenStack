import os, asyncio, yaml

def _throw(message: str) -> None: raise Exception(message)

async def _parallelForTask(f: int, t: int, s: int, c: callable) -> list[object]: [await c(idx) for idx in range(f, t, s)]
async def parallelFor(f: int, t: int, o: set, c: callable) -> list[object]: await asyncio.gather(*[_parallelForTask(f, t, i + 1, c) for i in range(o['max'] or 1)])

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
