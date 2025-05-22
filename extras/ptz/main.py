import tkinter as tk
from tkinter import ttk
from onvif import ONVIFCamera
import asyncio
import os

# Tk window
window = tk.Tk()
window.title("Лабораторная работа №2. ONVIF")
window.geometry('400x150')

# ONVIF functions
connection_status_var = tk.StringVar(window)
position_status_var = tk.StringVar(window)
connection_status = False
position_status = False
camera = None
async def connect(ip, port, login, password):
    global camera, connection_status_var, connection_status
    print(ip)
    camera = ONVIFCamera(ip, port, login, password, os.path.abspath("wsdl"))
    try:
        await camera.update_xaddrs()
        if camera is None:
            connection_status_var.set("Нет подключения")
            camera = None
            connection_status = False
        else:
            connection_status_var.set("Подключено к " + ip)
            connection_status = True
    except Exception as e:
        connection_status_var.set(e)
        camera = None
        connection_status = False
def sync_connect(ip, port, login, password):
    loop = asyncio.get_event_loop()
    loop.run_until_complete(connect(ip, port, login, password))
async def ptz_set(p, t, z):
    if (camera is not None) and (connection_status):
        try:
            await camera.update_xaddrs()
            ptz_service = await camera.create_ptz_service()
            media_service = await camera.create_media_service()
            profiles = await media_service.GetProfiles()
            req = ptz_service.create_type("AbsoluteMove")
            req.ProfileToken = profiles[0].token
            req.Position = {"PanTilt": {"x": p, "y": t}, "Zoom": {"x": z}}
            await ptz_service.AbsoluteMove(req)
            position_status_var.set("Положение камеры изменено")
            position_status = True
        except Exception as e:
            position_status_var.set(e)
            position_status = False
    else:
        position_status_var.set("Нет подключения")
        position_status = False
def sync_ptz_set(p, t, z):
    loop = asyncio.get_event_loop()
    loop.run_until_complete(ptz_set(p, t, z))
# Window contents
ip_label = tk.Label(window, text = "IP-адрес")
ip_label.grid(column=0, row=0)
ip_entry = tk.Entry(window, width=20)
ip_entry.grid(column=1, row=0)
ip_entry.insert(tk.END, "")
port_label = tk.Label(window, text = "Порт")
port_label.grid(column=0, row=1)
port_entry = tk.Entry(window, width=20)
port_entry.grid(column=1, row=1)
port_entry.insert(tk.END, "80")

login_label = tk.Label(window, text = "Логин")
login_label.grid(column=0, row=2)
login_entry = tk.Entry(window, width=20)
login_entry.grid(column=1, row=2)
login_entry.insert(tk.END, "admin")

password_label = tk.Label(window, text = "Пароль")
password_label.grid(column=0, row=3)
password_entry = tk.Entry(window, width=20, show='*')
password_entry.grid(column=1, row=3)
password_entry.insert(tk.END, "")

connection_status_var.set("Нет подключения")
def connect_button_clicked():
    sync_connect('http://'+ip_entry.get(), port_entry.get(), login_entry.get(), password_entry.get())
connect_button = tk.Button(window, text="Подключиться", command=connect_button_clicked)
connect_button.grid(column=0, columnspan=2, row=4)

connection_label = tk.Label(window, textvariable = connection_status_var)
connection_label.grid(column=0, columnspan=2, row=5)

s = ttk.Separator(window, orient="vertical")
s.grid(column=2, row=0, rowspan=6, sticky='ns')

p_label = tk.Label(window, text = "Pan")
p_label.grid(column=3, row=0)
p_entry = tk.Entry(window, width=20)
p_entry.grid(column=4, row=0)
p_entry.insert(tk.END, "0.7")

t_label = tk.Label(window, text = "Tilt")
t_label.grid(column=3, row=1)
t_entry = tk.Entry(window, width=20)
t_entry.grid(column=4, row=1)
t_entry.insert(tk.END, "0.7")

z_label = tk.Label(window, text = "Zoom")
z_label.grid(column=3, row=2)
z_entry = tk.Entry(window, width=20)
z_entry.grid(column=4, row=2)
z_entry.insert(tk.END, "0.0")

position_status_var.set("Нет подключения")
def position_button_clicked():
    try:
        sync_ptz_set(float(p_entry.get()), float(t_entry.get()), float(z_entry.get()))
    except ValueError as e:
        position_status_var.set(e)
position_button = tk.Button(window, text="Применить", command=position_button_clicked)
position_button.grid(column=3, columnspan=2, row=3)

position_label = tk.Label(window, textvariable = position_status_var)
position_label.grid(column=3, columnspan=2, row=4)

# Main loop
window.mainloop()