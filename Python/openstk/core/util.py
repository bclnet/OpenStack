import os, asyncio

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

#YamlDict
class YamlDict(dict):
    def __init__(self, file: str):
        self.path = f'{os.getenv("APPDATA")}{file}'
        if not os.path.isfile(self.path): return
        try:
            with open(self.path, 'r') as f:
                config_data = yaml.safe_load(f)
            # print(config_data)
            # print(f"Database host: {config_data['database']['host']}")
            # print(f"Server port: {config_data['server']['port']}")
        except FileNotFoundError: print(f'Error: {self.path} not found.')
        except yaml.YAMLError as e: print(f'Error parsing YAML file: {e}')

        # var items = (Dictionary<object, object>)new DeserializerBuilder()
        #     .WithNamingConvention(UnderscoredNamingConvention.Instance).Build()
        #     .Deserialize(File.ReadAllText(path));
        # foreach (var s in items) Add((string)s.Key, s.Value);