# https://stackoverflow.com/questions/74472798/how-to-define-python-generic-classes
from typing import Generic, TypeVar

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
    
    def get(self) -> None:
        return self.items.pop() if self.items else self.factory()

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