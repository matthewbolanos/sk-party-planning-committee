import os
import sys
import tty
import termios

# Function to get terminal response
def get_terminal_response(fd):
    response = b''
    while True:
        ch = os.read(fd, 1)
        response += ch
        if ch == b't':
            break
    return response

# Global cache for terminal size and cell size
terminal_cache = {
    'cell_width': None,
    'cell_height': None,
    'cols': None,
    'lines': None
}

# Function to get terminal size in characters (columns and lines)
def get_terminal_size_chars():
    cols, lines = os.get_terminal_size().columns, os.get_terminal_size().lines
    return cols, lines

# Function to get the dimensions of the terminal
def get_terminal_dimensions(command):
    # File descriptor for the standard input
    fd = sys.stdin.fileno()
    # Save the current terminal settings
    old_settings = termios.tcgetattr(fd)
    # Enable cbreak mode to avoid needing Enter key press
    tty.setcbreak(fd, termios.TCSANOW)
    print(command, end='', flush=True)
    response = get_terminal_response(fd)
    # Restore the terminal settings
    termios.tcsetattr(fd, termios.TCSANOW, old_settings)
    parts = response.decode().split(';')
    # Return the width and height found in the response
    return int(parts[2].rstrip('t')), int(parts[1])

# Function to get the width and height of a terminal cell in pixels
def get_cell_size(app):
    global terminal_cache

    # Get the current terminal dimensions in characters
    current_cols, current_lines = get_terminal_size_chars()

    # Check if we can use cached values
    if terminal_cache['cols'] == current_cols and terminal_cache['lines'] == current_lines and terminal_cache['cell_width'] and terminal_cache['cell_height']:
        return terminal_cache['cell_width'], terminal_cache['cell_height']

    # Suspend the application to send a terminal command
    with app.suspend():
        # Get the size of the terminal in pixels
        pixel_width, pixel_height = get_terminal_dimensions('\033[14t')

    
    # Calculate the size of each cell
    cell_width = pixel_width / current_cols
    cell_height = pixel_height / current_lines

    # Update the cache
    terminal_cache.update({
        'cell_width': cell_width,
        'cell_height': cell_height,
        'cols': current_cols,
        'lines': current_lines
    })

    return cell_width, cell_height