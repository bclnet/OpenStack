import time
from functools import wraps
from contextlib import contextmanager

def funcProfiler(func):
    @wraps(func)
    def wrapper(*args, **kwargs):
        start = time.perf_counter(); result = func(*args, **kwargs); end = time.perf_counter()
        print(f"Method '{func.__name__}' took {end - start:.6f} seconds.")
        return result
    return wrapper

@contextmanager
def ctxProfiler(label):
    start = time.perf_counter()
    try: yield
    finally:
        end = time.perf_counter()
        print(f"{label} executed in {end - start:.6f} seconds.")


# @funcProfiler
# def fast_method():
#     return sum(range(1000000))

# # Use it with a 'with' statement
# with ctxProfiler("Heavy Calculation"):
#     ans = sum(range(5000000))