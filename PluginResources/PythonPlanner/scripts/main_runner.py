import ast
import json
import traceback
import io
import contextlib

def serialize_exception(exception):
    """
    Converts an exception object to a dictionary for JSON serialization.

    Args:
        exception (Exception): The exception to serialize.

    Returns:
        dict: A dictionary containing the exception type, message, and traceback.
    """
    exc_type = type(exception).__name__
    exc_message = str(exception)
    exc_traceback = ''.join(traceback.format_tb(exception.__traceback__))
    return {
        'type': exc_type,
        'message': exc_message,
        'traceback': exc_traceback
    }


def is_print_call(expr):
    """
    Check if the expression is a print function call.

    Args:
        expr (ast.AST): The AST node to check.

    Returns:
        bool: True if the node is a print function call, False otherwise.
    """
    return (
        isinstance(expr, ast.Call)
        and isinstance(expr.func, ast.Name)
        and expr.func.id == 'print'
    )


def run_script_as_main(filepath):
    """
    Execute a Python script as the main program, capturing stdout and errors.

    Args:
        filepath (str): Path to the Python script to execute.
    """
    with open(filepath, 'r') as file:
        code = file.read()

    stdout_buffer = io.StringIO()
    stderr = None
    last_result = None
    local_vars = {}

    try:
        # Parse the code and identify the last expression
        parsed_code = ast.parse(code)
        if not parsed_code.body:
            write_to_file(stdout_buffer.getvalue(), stderr, last_result)
            return

        last_node = parsed_code.body[-1]
        last_expr = last_node.value if isinstance(last_node, ast.Expr) else None
        if last_expr and not is_print_call(last_expr):
            parsed_code.body.pop()
        else:
            last_expr = None

        compiled_code = compile(parsed_code, filename="<ast>", mode="exec")

        # Execute the script within a controlled output buffer
        with contextlib.redirect_stdout(stdout_buffer), contextlib.redirect_stderr(stdout_buffer):
            try:
                exec(compiled_code, local_vars, local_vars)
                if last_expr:
                    last_result = eval(compile(ast.Expression(last_expr), filename="<ast>", mode="eval"), local_vars, local_vars)
            except Exception as e:
                stderr = serialize_exception(e)
    except Exception as e:
        stderr = serialize_exception(e)

    write_to_file(stdout_buffer.getvalue(), stderr, last_result)


def write_to_file(stdout, stderr, result):
    """
    Write execution details to a file.

    Args:
        stdout (str): Captured standard output.
        stderr (str or None): Captured standard error, if any.
        result (any): Result of the last evaluated expression, if any.
    """
    termination_file_path = '/mnt/data/scripts/io/termination.txt'
    data = {
        'stdout': stdout,
        'stderr': stderr,
        'result': result
    }
    
    with open(termination_file_path, 'w') as f:
        f.write(json.dumps(data)+'\n')


# Execute the target script
run_script_as_main('/mnt/data/scripts/main.py')
