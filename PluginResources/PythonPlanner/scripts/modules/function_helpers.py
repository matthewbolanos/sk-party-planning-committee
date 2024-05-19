import os
import time
import json

def write_function_result(data):
    """
    Write the function results to individual files.

    Args:
        data (str): The JSON string containing the function results.
    """
    results_dir = '/mnt/data/scripts/io/function_results'

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
    directory = '/mnt/data/scripts/io/function_results'
    file_path = os.path.join(directory, f"{id}.txt")

    while not os.path.exists(file_path):
        time.sleep(0.01)

    with open(file_path, 'r') as f:
        contents = f.read()

    return json.loads(contents)
    

def write_function_call(plugin_name, function_name, args):
    """
    Write a function call to the function_calls.txt file.

    Args:
        plugin_name (str): The name of the plugin.
        function_name (str): The name of the function.
        args (dict): The arguments for the function.
    """
    file_path = '/mnt/data/scripts/io/function_calls.txt'

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
