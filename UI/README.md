# UI for the Party Planning Committee

The best part of the Party Planning Committee solution is that the entire chat UI can be
run directly in your terminal! This means you can interact with your agents and plan your
party from anywhere, whether that's in VS Code, a terminal window, or even a GitHub Codespace.

## We love the Assistant API
Over on the Semantic Kernel team, we believe that the OpenAI Assistant API is the future
protocol for all AI agents. That's why we've built our agents to mimic the Assistant API.
This has also allowed us to build our UI in a way that supports the Party Planning agents
_and_ any other agents that use the Assistant API.

## How to run the UI
The easiest way to run the UI is with the rest of the solution using `docker-compose`. However,
if you'd like to run the UI on its own (say for debug purposes), you can do so within VS Code
or GitHub Codespaces by running the `Run UI` launch configuration from the Run and Debug panel.

// Include gif of hitting the button

If you _really_ want to run the UI without VS Code or Codespaces, you can do so by running the
following command from this directory:

```bash
pip install -r requirements.txt
python main.py
```

Actually... that wasn't that hard either! 🎉

## Kudos where kudos are due
The front end looks as slick and retro as it does because of the awesome work done by the
[Rich]() and [Blessed]() libraries. We're so grateful for the work they've done to make
terminal applications look _so_ amazing!

We've extended their libraries to include some custom components that are specific to chat
applications. These include:


