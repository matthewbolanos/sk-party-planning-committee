import logging
import sys
import os

# Directory paths
cache_directory = "/mnt/data/cache"
response_folder = "/mnt/data/responses"
scripts_folder_name = "/mnt/data/scripts"
modules_folder_name = "/mnt/data/scripts/modules"
io_folder = "/mnt/data/scripts/io"
function_results_folder = "/mnt/data/scripts/io/function_results"

# Set environment variables
os.environ['NUMBA_CACHE_DIR'] = cache_directory
os.environ['PYTHONASYNCIODEBUG'] = '1'

# Folders to create
folders = [cache_directory, response_folder, scripts_folder_name, modules_folder_name, io_folder, function_results_folder]

# Create folders if they don't exist
for folder in folders:
    os.makedirs(folder, exist_ok=True)

# Add __init__.py files to the scripts and modules folders
init_files = [os.path.join(scripts_folder_name, "__init__.py"), os.path.join(modules_folder_name, "__init__.py")]
for init_file in init_files:
    with open(init_file, 'w') as file:
        file.write("")

# Add the scripts folder to the Python path
if scripts_folder_name not in sys.path:
    sys.path.append(scripts_folder_name)
