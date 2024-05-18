import logging
import os
import psutil
import subprocess

# Initialize logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

def run_script(file_path):
    """
    Run the Python script in a separate process using subprocess.

    Args:
        file_path (str): Path to the Python script to be executed.
    """
    try:
        subprocess.Popen(["python", file_path], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    except Exception as e:
        logger.error(f"Failed to start script {file_path}: {e}")

def find_and_terminate(script_name):
    """
    Find and terminate all processes running the given Python script.

    Args:
        script_name (str): The name of the Python script to be terminated.
    """
    terminated = False
    for process in psutil.process_iter(['pid', 'name', 'cmdline']):
        try:
            if process.info['cmdline'] and script_name in process.info['cmdline']:
                print(f"Terminating process {process.pid} running {script_name}")
                process.terminate()  # Graceful termination
                process.wait(timeout=5)
                terminated = True
        except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess) as e:
            logger.warning(f"Error terminating process: {e}")

    if not terminated:
        print(f"No running instances of {script_name} found.")
    else:
        print(f"All running instances of {script_name} have been terminated.")

if __name__ == "__main__":
    """
    Main entry point of the script.

    This block will:
    1. Define a list of Python scripts to manage.
    2. Find and terminate any running instances of those scripts.
    3. Start each script in a separate process.
    """

    # Clear all io files
    io_files = [
        "/mnt/data/scripts/io/last_function_call.txt",
        "/mnt/data/scripts/io/function_calls.txt"
    ]

    for file_path in io_files:
        with open(file_path, 'w') as file:
            file.write("")
    
    # Delete the termination file
    termination_file_path = '/mnt/data/scripts/io/termination.txt'
    if os.path.exists(termination_file_path):
        os.remove(termination_file_path)

    scripts_to_run = [
        "/mnt/data/scripts/main_runner.py"
    ]

    # Terminate all running instances of the scripts
    for script_name in scripts_to_run:
        find_and_terminate(script_name)
        run_script(script_name)

    print("Started the script execution process.")
