import time
from typing import Iterator, Generic, TypeVar # https://stackoverflow.com/questions/74472798/how-to-define-python-generic-classes
T = TypeVar('T')

class IGenericPool(Generic[T]):
    def get(self) -> None: pass
    def release(self, item: T) -> None: pass
    def action(self, action: callable) -> None: pass
    def func(self, action: callable) -> object: pass

class GenericPool(Generic[T]):
    def __init__(self, factory: callable, reset: callable = None, retainInPool: int = 10):
        self.items: list[T] = []
        self.factory: callable = factory
        self.reset: callable = reset
        self.retainInPool: int = retainInPool
    def __enter__(self): return self
    def __exit__(self, *args):
        for s in self.items: s.__exit__(*args)
    
    def get(self) -> None: return self.items.pop() if self.items else self.factory()

    def release(self, item: T) -> None:
        if len(self.items) < self.retainInPool: self.reset and self.reset(item); self.items.append(item)
        else: item.__exit__(None, None, None)

    def action(self, action: callable) -> None:
        item = self.get()
        try: action(item)
        finally: self.release(item)

    def func(self, action: callable) -> object:
        item = self.get()
        try: return action(item)
        finally: self.release(item)

class SinglePool(GenericPool, Generic[T]):
    def __init__(self, single: T, reset: callable = None): super().__init__(None, reset); self.single: T = single
    def get(self) -> None: return self.single
    def release(self, item: T) -> None: self.reset and self.reset(item); self.single.__exit__(None, None, None)

class StaticPool(GenericPool, Generic[T]):
    def __init__(self, single: T, reset: callable = None): super().__init__(None, reset); self.static: T = static
    def get(self) -> None: return self.static
    def release(self, item: T) -> None: self.reset and self.reset(item)

class CoroutineQueue:
    def __init__(self):
        self.tasks: list[Iterator] = []
        self.time = None
    def add(self, task: Iterator) -> Iterator: self.tasks.append(task); return task
    def cancel(self, task: Iterator) -> None: return self.tasks.remove(task)
    def clear(self) -> None: return self.tasks.clear()
    def run(self, desiredWorkTime: float) -> None:
        if len(self.tasks) > 0: return
        self.time = time.time()
        while len(self.tasks) > 0 and (time.time() - self.time) < desiredWorkTime:
            # try to execute an iteration of a task. remove the task if it's execution has completed.
            if not next(self.tasks[0]): self.pop()
    def waitFor(self, task: object) -> None:
        while next(task): self.tasks.remove(task)
    def waitForAll(self):
        for task in self.tasks:
            while next(task): pass
        self.tasks.clear()