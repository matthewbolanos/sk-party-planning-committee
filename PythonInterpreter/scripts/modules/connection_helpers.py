import json
import time
import os
import threading

terminate_event = threading.Event()
process_complete_event = threading.Event()

result = None
stdout = None
stderr = None

import os
import json
import time
from threading import Event

terminate_event = Event()
process_complete_event = Event()
result = None

def read_last_position(position_file):
    """
    Read the last read position from the position file.

    Args:
        position_file (str): Path to the file storing the last read position.

    Returns:
        int: The last read position.
    """
    if os.path.exists(position_file):
        try:
            with open(position_file, 'r') as file:
                position = file.read().strip()
                return int(position) if position else 0
        except Exception as e:
            return 0
    return 0

def write_last_position(position_file, position):
    """
    Write the last read position to the position file.

    Args:
        position_file (str): Path to the file storing the last read position.
        position (int): The position to store.
    """
    try:
        with open(position_file, 'w') as file:
            file.write(str(position))
    except Exception as e:
        pass

def tail_f(file_path, position_file):
    """
    Continuously read new lines appended to the given file.

    Args:
        file_path (str): Path to the file to tail.
        position_file (str): Path to the file storing the last read position.
    """
    last_position = read_last_position(position_file) or 0
    function_calls = []

    with open(file_path, 'r') as file:
        file.seek(last_position)

        start_time = None
        while not terminate_event.is_set():
            try:
                line = file.readline()
                if line:
                    function_call = json.loads(line.strip())
                    position = file.tell()
                    #function_call['id'] = last_position
                    function_calls.append(function_call)
                    write_last_position(position_file, position)

                    if start_time is None:
                        start_time = time.time()  # Start the timer after the first function call

                if start_time is not None and (time.time() - start_time >= 1):
                    global result
                    result = json.dumps(function_calls)
                    process_complete_event.set()
                    break

                time.sleep(0.01)  # Sleep briefly to avoid busy waiting
            except Exception as e:
                time.sleep(1)  # Sleep longer on error to prevent rapid retry



def watch_termination_file(termination_file_path):
    """
    Watch for the termination file and exit when it is detected.

    Args:
        termination_file_path (str): Path to the termination file.
    """
    global result, stdout, stderr

    while not terminate_event.is_set():
        if os.path.exists(termination_file_path):
            with open(termination_file_path, 'r') as file:
                termination_data = json.load(file)
                stdout = termination_data.get("stdout")
                stderr = termination_data.get("stderr")
                result = termination_data.get("result")
            process_complete_event.set()
            break
        time.sleep(0.01)  # Check for termination file every second

# Custom exception class to handle errors
class UniversalError(Exception):
    def __init__(self, message, error_type, traceback_info=None):
        self.message = message
        self.error_type = error_type
        self.traceback_info = traceback_info
        super().__init__(message)

    def __str__(self):
        if self.traceback_info:
            return f"{self.error_type}: {self.message}\nTraceback:\n{self.traceback_info}"
        return f"{self.error_type}: {self.message}"

def raise_exception(stderr):
    """Raises an exception based on serialized JSON error data without including this function in the traceback."""
    message = stderr['message']
    error_type = stderr['type']
    traceback_str = stderr.get('traceback', '')

    raise UniversalError(message, error_type, traceback_str) from None

def main_runner():
    global terminate_event, process_complete_event, result, stdout, stderr

    terminate_event = Event()
    process_complete_event = Event()
    result = None

    function_calls_file_path = '/mnt/data/scripts/io/function_calls.txt'
    termination_file_path = '/mnt/data/scripts/io/termination.txt'
    position_file_path = '/mnt/data/scripts/io/last_function_call.txt'

    # Create and start the thread for tailing the function calls file
    function_calls_thread = threading.Thread(target=tail_f, args=(function_calls_file_path, position_file_path))
    function_calls_thread.start()

    # Create and start the thread for watching the termination file
    termination_thread = threading.Thread(target=watch_termination_file, args=(termination_file_path,))
    termination_thread.start()

    # Wait for either thread to complete
    process_complete_event.wait()

    # Signal threads to terminate
    terminate_event.set()

    # Wait for the threads to complete
    function_calls_thread.join()
    termination_thread.join()

    # Perform the final logic for stdout and stderr processing
    if stdout is not None:
        lines = stdout.split('\n')

        # Remove trailing empty lines
        while lines and not lines[-1]:
            lines.pop()
        
        for line in lines:
            print(line, end='')

            # if it's not the last line, add a newline
            if line != lines[-1]:
                print()

    if stderr is not None:
        try:
            raise_exception(stderr)
        except UniversalError as e:
            tb = e.__traceback__
            next_tb = tb
            while next_tb.tb_next.tb_next is not None:
                next_tb = next_tb.tb_next
            next_tb.tb_next = None
            raise e.with_traceback(tb) from None 
        
    return result

