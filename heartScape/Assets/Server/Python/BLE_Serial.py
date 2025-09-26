import asyncio
from bleak import BleakClient
from functools import partial
import websockets # ★★★ WebSocketライブラリをインポート

# --- 設定項目 ---
TARGET_DEVICES = {
    "M5_A": "48:27:e2:e3:c8:e9",  # 1台目のアドレス
    "M5_B": "48:27:e2:e3:b6:55"   # 2台目のアドレス
}
NOTIFY_CHARACTERISTIC_UUID = "beb5483e-36e1-4688-b7f5-ea07361b26a8"
RECONNECT_DELAY_SECONDS = 3  # 再接続を試みるまでの待機時間（秒）

WEBSOCKET_HOST = "localhost" # サーバーのホスト名
WEBSOCKET_PORT = 8765        # サーバーのポート番号
# --- ここまで ---


# ★★★ WebSocket関連のグローバル変数 ★★★
# 接続されている全Unityクライアントを保持するセット
CONNECTED_CLIENTS = set()


# ★★★ 1. WebSocketクライアントにメッセージをブロードキャストする関数 ★★★
async def broadcast_to_clients(message):
    """接続されている全クライアントにメッセージを送信する"""
    if CONNECTED_CLIENTS:
        # 全てのクライアントへの送信タスクを作成し、並行して実行
        await asyncio.gather(
            *(client.send(message) for client in CONNECTED_CLIENTS)
        )

# ★★★ 2. BLEの通知ハンドラを修正 ★★★
#    受信したデータをprintするだけでなく、WebSocketでブロードキャストする
def notification_handler(address, sender, data):
    """BLEでデータを受信したら、コンソールに表示し、Unityクライアントに送信する"""
    message = data.decode('utf-8')
    print(f"[{address}] から受信: {message}")
    
    # asyncioのイベントループにブロードキャストタスクをスケジュールする
    # これにより、WebSocketの送信処理がBLEの受信処理をブロックしない
    asyncio.create_task(broadcast_to_clients(message))


# ★★★ 3. WebSocketサーバーのハンドラ関数 ★★★
async def websocket_handler(websocket, path):
    """Unityクライアントが接続したときの処理"""
    print(f"Unityクライアントが接続しました: {websocket.remote_address}")
    CONNECTED_CLIENTS.add(websocket)
    try:
        # クライアントが接続している間、メッセージを待ち続ける
        # Unity側からメッセージを送る必要がなければ、このループは実質何もしない
        async for message in websocket:
            print(f"Unityから受信: {message}") # Unityからのメッセージは基本使わない
    except websockets.exceptions.ConnectionClosed:
        print(f"Unityクライアントの接続が切れました: {websocket.remote_address}")
    finally:
        # 接続が切れたらセットから削除
        CONNECTED_CLIENTS.remove(websocket)


async def connect_and_listen(address, name):
    """（この関数は変更なし）"""
    while True:
        client = None
        try:
            print(f"[{name}] {address} への接続を試みます...")
            def on_disconnect(client):
                print(f"警告: [{name}] {client.address} との接続が切れました。")
            
            client = BleakClient(address, disconnected_callback=on_disconnect, timeout=20.0)
            await client.connect()

            if client.is_connected:
                print(f"◎ [{name}] {address} に接続成功")
                handler = partial(notification_handler, client.address)
                await client.start_notify(NOTIFY_CHARACTERISTIC_UUID, handler)
                print(f"   -> [{name}] からの通知待受を開始しました。")
                while client.is_connected:
                    await asyncio.sleep(1)
        except Exception as e:
            print(f"× エラー: [{name}] {address} への接続中に問題が発生しました: {e}")
        finally:
            if client and client.is_connected:
                await client.disconnect()
            print(f"[{name}] {RECONNECT_DELAY_SECONDS}秒後に再接続します。")
            await asyncio.sleep(RECONNECT_DELAY_SECONDS)


async def main():
    print("複数のM5Stackへの接続とWebSocketサーバーを起動します...")

    # ★★★ 4. WebSocketサーバーを起動する ★★★
    start_server = websockets.serve(websocket_handler, WEBSOCKET_HOST, WEBSOCKET_PORT)
    
    # BLE接続用のタスクを作成
    ble_tasks = [
        connect_and_listen(address, name)
        for name, address in TARGET_DEVICES.items()
    ]
    
    # ★★★ 5. WebSocketサーバーとBLEクライアントの両方を並行して実行する ★★★
    await asyncio.gather(
        start_server,
        *ble_tasks
    )


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nプログラムを終了します。")