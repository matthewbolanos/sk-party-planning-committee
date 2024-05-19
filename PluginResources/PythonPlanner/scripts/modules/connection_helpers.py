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

def main_runner():
    global terminate_event, process_complete_event, result, stdout, stderr

    terminate_event = Event()
    process_complete_event = Event()
    result = None

    function_calls_file_path = '/mnt/data/.io/function_calls.txt'
    termination_file_path = '/mnt/data/.io/termination.txt'
    position_file_path = '/mnt/data/.io/last_function_call.txt'

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

def write_function_call(plugin_name, function_name, args):
    """
    Write a function call to the function_calls.txt file.

    Args:
        plugin_name (str): The name of the plugin.
        function_name (str): The name of the function.
        args (dict): The arguments for the function.
    """
    file_path = '/mnt/data/.io/function_calls.txt'

    # Get the current last position of the file
    position = 0
    if os.path.exists(file_path) and os.path.getsize(file_path) > 0:
        with open(file_path, 'r') as f:
            try:
                # Read the last line
                f.seek(-2, os.SEEK_END)
                while f.read(1) != b'\n':
                    f.seek(-2, os.SEEK_CUR)
                last_line = f.readline().decode()
                last_entry = json.loads(last_line)
                position = int(last_entry['id']) + 1
            except OSError:
                # If file is too small, just read normally
                f.seek(0)
                last_line = f.readlines()[-1]
                last_entry = json.loads(last_line)
                position = int(last_entry['id']) + 1

    function_call = {
        'id': position,
        'plugin_name': plugin_name,
        'function_name': function_name,
        'args': json.dumps(args)
    }

    # Write the new function call to the file
    with open(file_path, 'a') as f:
        f.write(json.dumps(function_call) + '\n')

    return position

def write_function_result(data):
    """
    Write the function results to individual files.

    Args:
        data (str): The JSON string containing the function results.
    """
    results_dir = '/mnt/data/.io/function_results'

    for function_result in data:
        # request = {
        #     "id": function_result['id'],
        #     "result": function_result['result']
        # }
        file_path = os.path.join(results_dir, f"{function_result['id']}.txt")

        # Write to the specific file corresponding to the ID
        with open(file_path, 'w') as f:
            f.write(json.dumps(function_result['result']) + '\n')

def poll_for_results(id):
    """
    Poll for the existence of a file based on the ID and return its contents once it exists.

    Args:
        id (int): The ID to look for.

    Returns:
        dict: The contents of the file as a dictionary.
    """
    directory = '/mnt/data/.io/function_results'
    file_path = os.path.join(directory, f"{id}.txt")

    while not os.path.exists(file_path):
        time.sleep(0.01)

    with open(file_path, 'r') as f:
        contents = f.read()

    return json.loads(contents)
    



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
        time.sleep(0.01)

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