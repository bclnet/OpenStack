
# GlobalTime
class GlobalTime:
    ticks: int
    delta: float

# Plugin
class Plugin:
    plugin: list['Plugin']
    path: str
    isValid: bool
    @staticmethod
    def create(path: str) -> 'Plugin': return None
    @staticmethod
    def onClosing() -> None: pass
    @staticmethod
    def onFocusGained() -> None: pass
    @staticmethod
    def onFocusLost() -> None: pass
    #@staticmethod
    #def onConnected() -> None: pass
    #@staticmethod
    #def onDisconnected() -> None: pass
    @staticmethod
    def processHotkeys(key: int, mod: int, ispressed: int) -> bool: return True
    @staticmethod
    def processMouse(button: int, wheel: int) -> bool: return True
    @staticmethod
    def processDrawCmdList(device: object) -> None: pass
    @staticmethod
    def processWndProc(e: object) -> int: return 0
    # @staticmethod
    # def updatePlayerPosition(int x, int y, int z) -> None: pass
    @staticmethod
    def tick() -> None: pass

# IPluginHost
class IPluginHost:
    def initialize(self) -> None: pass
    def loadPlugin(self, pluginPath: str) -> None: pass
    def tick(self) -> None: pass
    def closing(self) -> None: pass
    def focusGained(self) -> None: pass
    def focusLost(self) -> None: pass
    def connected(self) -> None: pass
    def disconnected(self) -> None: pass
    def hotkey(self, key: int, mod: int, pressed: bool) -> bool: pass
    def mouse(self, button: int, wheel: int) -> None: pass
    def getCommandList(self, listPtr: int, listCount: int) -> None: pass
    def event(self, ev: object) -> int: pass
    def updatePlayerPosition(self, x: int, y: int, z: int) -> None: pass
    def packetIn(self, buffer: bytes) -> bool: pass
    def packetOut(self, buffer: bytes) -> bool: pass

# IClientHost
class IClientHost:
    def dispose(self) -> None: pass
    def run(self) -> None: pass

# ClientBase
class ClientBase:
    def dispose(self) -> None: pass
    def loadContent(self) -> None: pass
    def unloadContent(self) -> None: pass

# SceneBase
class SceneBase:
    isDestroyed: bool
    isLoaded: bool
    def dispose(self) -> None:
        if self.isDestroyed: return
        self.unload()
        self.isDestroyed = True
    def update(self) -> None: pass # Camera.Update(true, Time.Delta, Mouse.Position);
    def draw(self) -> bool: return True
    def load(self) -> None: self.isLoaded = True
    def unload(self) -> None: self.isLoaded = False
    # input
    # def onMouseUp(MouseButtonType button) -> bool: return False
    # def onMouseDown(MouseButtonType button) -> bool: return False
    # def onMouseDoubleClick(MouseButtonType button) -> bool: return False
    # def onMouseWheel(self, up: bool) -> bool: return False
    def onMouseDragging() -> bool: return False
    def onTextInput(self, text: str) -> None: pass
    # def onKeyDown(self, e: object) -> None: pass
    # def onKeyUp(self, e: object) -> None: pass


