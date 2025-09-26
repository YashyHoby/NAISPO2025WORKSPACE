import argparse
import asyncio
import json
import sys
from functools import partial

from bleak import BleakClient
import websockets

# --- Configuration ---
TARGET_DEVICES = {
    "M5_A": "48:27:e2:e3:c8:e9",
    "M5_B": "48:27:e2:e3:b6:55",
}
NOTIFY_CHARACTERISTIC_UUID = "beb5483e-36e1-4688-b7f5-ea07361b26a8"
RECONNECT_DELAY_SECONDS = 3

WEBSOCKET_HOST = "localhost"
WEBSOCKET_PORT = 8765
# --- end configuration ---

CONNECTED_CLIENTS = set()

DEBUG_SAMPLE_PAYLOADS = {
    "1": {"button": 0, "state": "released", "time": 2, "hr": 60, "cv": 0.045, "dynRange": 0.35, "Hue": 20},
    "2": {"button": 1, "state": "released", "time": 0.75, "hr": 72, "cv": 0.085, "dynRange": 0.55, "Hue": 135},
    "3": {"button": 2, "state": "released", "time": 1.20, "hr": 88, "cv": 0.120, "dynRange": 0.72, "Hue": 220},
    "4": {"button": 3, "state": "released", "time": 1.80, "hr": 102, "cv": 0.160, "dynRange": 0.90, "Hue": 315},
}


async def broadcast_to_clients(message: str) -> None:
    if CONNECTED_CLIENTS:
        await asyncio.gather(*(client.send(message) for client in CONNECTED_CLIENTS))


def notification_handler(address, sender, data: bytearray):
    message = data.decode("utf-8")
    print(f"[{address}] received: {message}")
    asyncio.create_task(broadcast_to_clients(message))


async def websocket_handler(websocket, path=None):
    print(f"Unity client connected: {websocket.remote_address}")
    CONNECTED_CLIENTS.add(websocket)
    try:
        async for message in websocket:
            print(f"Message from Unity: {message}")
    except websockets.exceptions.ConnectionClosed:
        print(f"Unity client disconnected: {websocket.remote_address}")
    finally:
        CONNECTED_CLIENTS.discard(websocket)


async def connect_and_listen(address: str, name: str):
    while True:
        client = None
        try:
            print(f"[{name}] connecting to {address} ...")

            def on_disconnect(disconnected_client):
                print(f"Warning [{name}] connection lost: {disconnected_client.address}")

            client = BleakClient(address, disconnected_callback=on_disconnect, timeout=20.0)
            await client.connect()

            if client.is_connected:
                print(f"[{name}] connected to {address}")
                handler = partial(notification_handler, client.address)
                await client.start_notify(NOTIFY_CHARACTERISTIC_UUID, handler)
                print(f"   -> [{name}] notifications started")
                while client.is_connected:
                    await asyncio.sleep(1)
        except Exception as exc:
            print(f"Error [{name}] connecting to {address}: {exc}")
        finally:
            if client and client.is_connected:
                await client.disconnect()
            print(f"[{name}] retrying in {RECONNECT_DELAY_SECONDS} seconds")
            await asyncio.sleep(RECONNECT_DELAY_SECONDS)


async def debug_input_loop():
    loop = asyncio.get_running_loop()
    print("[Debug] Press keys 1-4 then Enter to send sample packets. q or empty line to quit.")
    try:
        while True:
            line = await loop.run_in_executor(None, sys.stdin.readline)
            if not line:
                break
            key = line.strip()
            if key in ("q", "Q", ""):
                print("[Debug] Leaving debug input loop.a")
                break
            if key in DEBUG_SAMPLE_PAYLOADS:
                payload = DEBUG_SAMPLE_PAYLOADS[key]
                message = json.dumps(payload)
                print(f"[Debug] sending: {message}")
                await broadcast_to_clients(message)
            else:
                print("[Debug] Enter 1-4 or q to quit.")
    except asyncio.CancelledError:
        pass


async def main(args):
    host = args.host or WEBSOCKET_HOST
    port = args.port or WEBSOCKET_PORT
    print(f"Starting BLE bridge and WebSocket server on {host}:{port}")

    server = await websockets.serve(websocket_handler, host, port)
    tasks = []
    try:
        if args.debug:
            print("[Debug] Running in debug mode (BLE disabled)")
            tasks.append(asyncio.create_task(debug_input_loop()))
        else:
            for name, address in TARGET_DEVICES.items():
                tasks.append(asyncio.create_task(connect_and_listen(address, name)))

        if tasks:
            await asyncio.gather(*tasks)
        else:
            await asyncio.Event().wait()
    finally:
        server.close()
        await server.wait_closed()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="BLE relay & WebSocket bridge")
    parser.add_argument("--host", default=WEBSOCKET_HOST, help="WebSocket host to bind (default: localhost)")
    parser.add_argument("--port", type=int, default=WEBSOCKET_PORT, help="WebSocket port to bind (default: 8765)")
    parser.add_argument("--debug", action="store_true", help="Debug mode: send sample packets from keyboard")
    cli_args = parser.parse_args()

    try:
        asyncio.run(main(cli_args))
    except KeyboardInterrupt:
        print("\nExiting.")
