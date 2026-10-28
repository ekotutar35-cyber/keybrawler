"""Windows screen OCR assistant. No game memory access or injection."""
import ctypes
from ctypes import wintypes
import queue
import sys
import threading
import time
import tkinter as tk
from tkinter import ttk, messagebox, simpledialog

from bubbles import group_lines

if sys.platform != 'win32':
    raise SystemExit('Bu uygulama Windows üzerinde çalışır.')

import numpy as np
from PIL import ImageGrab
from rapidocr_onnxruntime import RapidOCR

user32 = ctypes.windll.user32
user32.SetProcessDPIAware()
user32.GetForegroundWindow.restype = wintypes.HWND
user32.WindowFromPoint.argtypes = [wintypes.POINT]
user32.WindowFromPoint.restype = wintypes.HWND
user32.GetAncestor.argtypes = [wintypes.HWND, wintypes.UINT]
user32.GetAncestor.restype = wintypes.HWND
ULONG_PTR = ctypes.c_size_t


class KEYBDINPUT(ctypes.Structure):
    _fields_ = [('wVk', wintypes.WORD), ('wScan', wintypes.WORD),
                ('dwFlags', wintypes.DWORD), ('time', wintypes.DWORD),
                ('dwExtraInfo', ULONG_PTR)]


class MOUSEINPUT(ctypes.Structure):
    _fields_ = [('dx', wintypes.LONG), ('dy', wintypes.LONG),
                ('mouseData', wintypes.DWORD), ('dwFlags', wintypes.DWORD),
                ('time', wintypes.DWORD), ('dwExtraInfo', ULONG_PTR)]


class PAYLOAD(ctypes.Union):
    _fields_ = [('ki', KEYBDINPUT), ('mi', MOUSEINPUT)]


class INPUT(ctypes.Structure):
    _fields_ = [('type', wintypes.DWORD), ('data', PAYLOAD)]


def key(vk=0, scan=0, flags=0):
    event = INPUT(type=1, data=PAYLOAD(ki=KEYBDINPUT(vk, scan, flags, 0, 0)))
    if user32.SendInput(1, ctypes.byref(event), ctypes.sizeof(INPUT)) != 1:
        raise RuntimeError('Windows tuş gönderimini engelledi. Oyun ve uygulama aynı yetki seviyesinde olmalı.')


class App:
    def __init__(self, root):
        self.root = root
        self.region = self.target = None
        self.active = threading.Event()
        self.events = queue.Queue()
        self.seen = set()
        self.generation = 0
        self.delay = tk.StringVar(value='0.8')
        self.merge = tk.StringVar(value='12')
        root.title('KeyBrawler — baloncuk okuyucu')
        root.geometry('650x480')
        frame = ttk.Frame(root, padding=16)
        frame.pack(fill='both', expand=True)
        ttk.Label(frame, text='FFXIV pencere veya kenarlıksız pencere modunda olmalı.').pack(anchor='w')
        ttk.Button(frame, text='1. Baloncuk bölgesini seç (sürükle)', command=self.select_region).pack(fill='x', pady=6)
        ttk.Button(frame, text='2. Oyundaki yazı alanını seç (tıkla)', command=self.select_target).pack(fill='x')
        ttk.Button(frame, text='Seçim açılmıyorsa: koordinatları elle gir', command=self.manual).pack(fill='x', pady=4)
        row = ttk.Frame(frame)
        row.pack(fill='x', pady=10)
        ttk.Label(row, text='Onaylar arası saniye:').pack(side='left')
        ttk.Entry(row, textvariable=self.delay, width=6).pack(side='left')
        ttk.Label(row, text='  Satır birleştirme aralığı (px):').pack(side='left')
        ttk.Entry(row, textvariable=self.merge, width=6).pack(side='left')
        ttk.Button(frame, text='F8 Başlat / Durdur', command=self.toggle).pack(fill='x')
        ttk.Button(frame, text='F9 Acil durdur', command=self.stop).pack(fill='x', pady=4)
        ttk.Label(frame, text='Her metin oturumda bir kez gönderilir. Yeniden başlatmak geçmişi temizler.').pack(anchor='w')
        self.log = tk.Text(frame, height=12, state='disabled')
        self.log.pack(fill='both', expand=True, pady=8)
        self.ocr = None
        threading.Thread(target=self.worker, daemon=True).start()
        root.after(80, self.tick)
        root.protocol('WM_DELETE_WINDOW', self.close)

    def report(self, text):
        self.events.put(text)

    def stop(self):
        self.active.clear()
        self.generation += 1
        self.report('Durduruldu.')

    def close(self):
        self.stop()
        self.root.destroy()

    def overlay(self, point=False):
        self.stop()
        win = tk.Toplevel(self.root)
        win.attributes('-fullscreen', True)
        win.attributes('-topmost', True)
        win.attributes('-alpha', 0.35)
        canvas = tk.Canvas(win, bg='black', cursor='crosshair')
        canvas.pack(fill='both', expand=True)
        start = []
        rect = [None]

        def down(event):
            start[:] = [event.x_root, event.y_root]
            rect[0] = canvas.create_rectangle(event.x, event.y, event.x, event.y, outline='red', width=3)

        def move(event):
            if start:
                canvas.coords(rect[0], start[0], start[1], event.x_root, event.y_root)

        def up(event):
            if point:
                self.target = (event.x_root, event.y_root)
                self.game_window = user32.GetAncestor(user32.WindowFromPoint(wintypes.POINT(*self.target)), 2)
                # Resolve the game window after removing our overlay.
                win.destroy()
                self.root.after(200, self.resolve_target)
            elif start:
                region = (min(start[0], event.x_root), min(start[1], event.y_root),
                          max(start[0], event.x_root), max(start[1], event.y_root))
                if region[2] - region[0] > 10 and region[3] - region[1] > 10:
                    self.region = region
                    self.report(f'OCR bölgesi: {region}')
                win.destroy()

        canvas.bind('<ButtonPress-1>', down)
        canvas.bind('<B1-Motion>', move)
        canvas.bind('<ButtonRelease-1>', up)
        win.bind('<Escape>', lambda _: win.destroy())

    def resolve_target(self):
        self.game_window = user32.GetAncestor(user32.WindowFromPoint(wintypes.POINT(*self.target)), 2)
        self.report(f'Yazı alanı: {self.target}. Başlatmadan önce oyuna odaklan.')

    def select_region(self):
        self.overlay()

    def select_target(self):
        self.overlay(point=True)

    def manual(self):
        self.stop()
        value = simpledialog.askstring('Elle seçim',
            'Ana ekran piksel koordinatları: sol,üst,sağ,alt,yazıX,yazıY\nÖrnek: 100,100,900,500,600,800')
        if value is None:
            return
        try:
            left, top, right, bottom, x, y = map(int, value.split(','))
            if not (0 <= left < right <= self.root.winfo_screenwidth()
                    and 0 <= top < bottom <= self.root.winfo_screenheight()
                    and 0 <= x < self.root.winfo_screenwidth()
                    and 0 <= y < self.root.winfo_screenheight()):
                raise ValueError()
        except ValueError:
            messagebox.showerror('Geçersiz koordinatlar', 'Ana ekran sınırları içinde altı sayı girin.')
            return
        self.region, self.target = (left, top, right, bottom), (x, y)
        self.root.iconify()
        self.root.after(1500, self.resolve_target)
        self.report('Koordinatlar kaydedildi. Oyunu öne getirin; 1,5 saniye sonra hedef pencere belirlenecek.')

    def toggle(self):
        if self.active.is_set():
            self.stop()
            return
        if not self.region or not self.target:
            messagebox.showwarning('Seçim gerekli', 'Önce baloncuk bölgesini ve yazı alanını seç.')
            return
        try:
            self.interval = max(0.3, float(self.delay.get()))
            self.gap = max(0, int(self.merge.get()))
        except ValueError:
            messagebox.showerror('Geçersiz değer', 'Süre ve aralık sayısal olmalı.')
            return
        self.seen.clear()
        self.generation += 1
        self.active.set()
        self.report('Başlatıldı. Yalnızca seçilen oyun penceresi odaktayken gönderilir.')

    def permitted(self, generation):
        return (self.active.is_set() and generation == self.generation
                and user32.GetForegroundWindow() == self.game_window
                and not user32.GetAsyncKeyState(0x78) & 0x8000)

    def worker(self):
        while True:
            time.sleep(0.15)
            if not self.active.is_set():
                continue
            generation = self.generation
            try:
                if self.ocr is None:
                    self.report('OCR modeli yükleniyor…')
                    self.ocr = RapidOCR()
                if not self.permitted(generation):
                    continue
                results, _ = self.ocr(np.array(ImageGrab.grab(bbox=self.region).convert('RGB'))[:, :, ::-1])
                groups = group_lines(results, self.gap)
                # Send only one bubble, then capture again: the game may move all bubbles after Enter.
                for _, _, _, _, text in groups:
                    if text.casefold() in self.seen:
                        continue
                    if not self.permitted(generation):
                        break
                    user32.SetCursorPos(*self.target)
                    user32.mouse_event(2, 0, 0, 0, 0)
                    user32.mouse_event(4, 0, 0, 0, 0)
                    time.sleep(0.1)
                    for offset in range(0, len(text.encode('utf-16-le')), 2):
                        if not self.permitted(generation):
                            raise RuntimeError('Gönderim kesildi; yazı alanındaki kısmi metni temizleyin.')
                        unit = int.from_bytes(text.encode('utf-16-le')[offset:offset+2], 'little')
                        key(scan=unit, flags=4)
                        key(scan=unit, flags=6)
                        time.sleep(0.015)
                    if not self.permitted(generation):
                        raise RuntimeError('Onay öncesi durduruldu; yazı alanını kontrol edin.')
                    key(vk=13)
                    key(vk=13, flags=2)
                    self.seen.add(text.casefold())
                    self.report(f'Gönderildi: {text}')
                    deadline = time.monotonic() + self.interval
                    while self.permitted(generation) and time.monotonic() < deadline:
                        time.sleep(0.05)
                    break
            except Exception as exc:
                self.active.clear()
                self.report(f'Hata: {exc}')

    def tick(self):
        if user32.GetAsyncKeyState(0x77) & 1:
            self.toggle()
        if user32.GetAsyncKeyState(0x78) & 1:
            self.stop()
        while not self.events.empty():
            self.log.configure(state='normal')
            self.log.insert('end', self.events.get_nowait() + '\n')
            self.log.see('end')
            self.log.configure(state='disabled')
        self.root.after(80, self.tick)


if __name__ == '__main__':
    root = tk.Tk()
    App(root)
    root.mainloop()
