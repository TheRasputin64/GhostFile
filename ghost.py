import os
import threading
import customtkinter as ctk
from tkinter import filedialog, ttk
from queue import Queue
import time

class GhostRetro(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("GHOST_RETRO v1.0")
        self.geometry("1280x720")
        self.resizable(False, False)
        
        self.colors = {
            "bg": "#0a0a0f",
            "accent1": "#00ff9f",
            "accent2": "#ff0055",
            "accent3": "#00ccff",
            "panel1": "#151520",
            "panel2": "#1a1a25",
            "button": "#252535",
            "button_hover": "#353550",
            "input": "#0f0f15",
            "border": "#303045",
            "text": "#ffffff",
            "text_dim": "#8888aa"
        }
        
        self.stats = {"files": 0, "dirs": 0, "done": 0}
        self.log_queue = Queue()
        self.configure(fg_color=self.colors["bg"])
        
        self.setup_styles()
        self.setup_ui()
        self.process_logs()
        
    def setup_styles(self):
        style = ttk.Style()
        style.configure(
            "Cyberpunk.Treeview",
            background=self.colors["panel1"],
            foreground=self.colors["text"],
            fieldbackground=self.colors["panel1"],
            borderwidth=0
        )
        style.map("Cyberpunk.Treeview",
            background=[("selected", self.colors["accent1"])],
            foreground=[("selected", self.colors["bg"])]
        )
        
    def setup_ui(self):
        self.grid_columnconfigure(0, weight=3)
        self.grid_columnconfigure(1, weight=2)
        self.grid_rowconfigure(0, weight=1)
        
        left_frame = self.create_left_panel()
        right_frame = self.create_right_panel()
        
        self.add_decorative_elements(left_frame)
        self.add_decorative_elements(right_frame)
        
    def create_left_panel(self):
        left_frame = ctk.CTkFrame(self, fg_color=self.colors["panel1"], border_color=self.colors["border"], border_width=1)
        left_frame.grid(row=0, column=0, sticky="nsew", padx=10, pady=10)
        
        header = ctk.CTkLabel(left_frame, text="FILE SYSTEM MATRIX", font=("Rajdhani", 16, "bold"), text_color=self.colors["accent1"])
        header.pack(pady=10)
        
        self.tree = ttk.Treeview(left_frame, style="Cyberpunk.Treeview", height=25)
        self.tree.pack(fill="both", expand=True, padx=5, pady=5)
        
        return left_frame
        
    def create_right_panel(self):
        right_frame = ctk.CTkFrame(self, fg_color=self.colors["panel2"], border_color=self.colors["border"], border_width=1)
        right_frame.grid(row=0, column=1, sticky="nsew", padx=10, pady=10)
        right_frame.grid_columnconfigure(0, weight=1)
        
        self.create_status_panel(right_frame)
        self.create_control_panel(right_frame)
        self.create_log_panel(right_frame)
        
        return right_frame
        
    def create_status_panel(self, parent):
        status_frame = ctk.CTkFrame(parent, fg_color=self.colors["panel1"], height=40)
        status_frame.pack(fill="x", padx=5, pady=5)
        
        self.status = ctk.CTkLabel(status_frame, text="SYSTEM READY", font=("Rajdhani", 14), text_color=self.colors["accent3"])
        self.status.pack(expand=True)
        
    def create_control_panel(self, parent):
        control_frame = ctk.CTkFrame(parent, fg_color=self.colors["panel1"])
        control_frame.pack(fill="x", padx=5, pady=5)
        
        self.paths = []
        labels = ["SOURCE", "DESTINATION"]
        
        for i, label in enumerate(labels):
            path_var = ctk.StringVar()
            self.paths.append(path_var)
            
            input_frame = ctk.CTkFrame(control_frame, fg_color="transparent", height=35)
            input_frame.pack(fill="x", padx=10, pady=5)
            
            ctk.CTkLabel(input_frame, text=label, font=("Rajdhani", 12), text_color=self.colors["text_dim"], width=100).pack(side="left", padx=5)
            
            entry = ctk.CTkEntry(input_frame, textvariable=path_var, font=("Consolas", 12), fg_color=self.colors["input"], text_color=self.colors["text"], border_color=self.colors["border"], border_width=1, height=30)
            entry.pack(side="left", fill="x", expand=True, padx=5)
            
            cmd = self.select_source if i == 0 else self.select_destination
            ctk.CTkButton(input_frame, text="SELECT", command=cmd, font=("Rajdhani", 12), fg_color=self.colors["button"], hover_color=self.colors["button_hover"], text_color=self.colors["accent1"], width=80, height=30).pack(side="right", padx=5)
        
        self.execute_btn = ctk.CTkButton(control_frame, text="EXECUTE", command=self.start_operation, font=("Rajdhani", 14, "bold"), fg_color=self.colors["accent2"], hover_color=self.colors["button_hover"], text_color=self.colors["text"], width=150, height=35)
        self.execute_btn.pack(pady=15)
        
    def create_log_panel(self, parent):
        log_frame = ctk.CTkFrame(parent, fg_color=self.colors["panel1"])
        log_frame.pack(fill="both", expand=True, padx=5, pady=5)
        
        ctk.CTkLabel(log_frame, text="SYSTEM LOG", font=("Rajdhani", 14), text_color=self.colors["accent3"]).pack(pady=5)
        
        self.log_text = ctk.CTkTextbox(log_frame, font=("Consolas", 12), fg_color=self.colors["input"], text_color=self.colors["text"], border_color=self.colors["border"], border_width=1)
        self.log_text.pack(fill="both", expand=True, padx=5, pady=5)
        
        self.progress = ctk.CTkProgressBar(log_frame, height=4, progress_color=self.colors["accent1"], fg_color=self.colors["input"])
        self.progress.pack(fill="x", padx=5, pady=5)
        self.progress.set(0)
        
    def add_decorative_elements(self, frame):
        for pos in ["ne", "nw", "se", "sw"]:
            accent = ctk.CTkFrame(frame, fg_color=self.colors["accent1"], width=2, height=10 if pos[0] == "n" else 10)
            accent.place(relx=0 if "w" in pos else 1, rely=0 if "n" in pos else 1, anchor=pos)

    def log(self, msg):
        self.log_queue.put(f"[{time.strftime('%H:%M:%S')}] {msg}")
        
    def process_logs(self):
        try:
            while True:
                msg = self.log_queue.get_nowait()
                self.log_text.insert("end", msg + "\n")
                self.log_text.see("end")
        except:
            pass
        self.after(100, self.process_logs)
        
    def scan_directory(self, path):
        self.stats["files"] = 0
        self.stats["dirs"] = 0
        
        for _, dirs, files in os.walk(path):
            self.stats["dirs"] += len(dirs)
            self.stats["files"] += len(files)
            self.status.configure(text=f"FILES: {self.stats['files']} | DIRECTORIES: {self.stats['dirs']}")
            
    def select_source(self):
        folder = filedialog.askdirectory()
        if folder:
            self.paths[0].set(folder)
            self.log(f"Source selected: {folder}")
            threading.Thread(target=lambda: self.scan_directory(folder), daemon=True).start()
            self.update_tree(folder)
            
    def select_destination(self):
        folder = filedialog.askdirectory()
        if folder:
            self.paths[1].set(folder)
            self.log(f"Destination selected: {folder}")
            
    def update_tree(self, path):
        for item in self.tree.get_children():
            self.tree.delete(item)
            
        def add_node(parent, path):
            try:
                items = os.scandir(path)
                for item in items:
                    node = self.tree.insert(parent, "end", text=item.name)
                    if item.is_dir():
                        add_node(node, item.path)
            except Exception as e:
                self.log(f"Error: {e}")
                
        threading.Thread(target=lambda: add_node("", path), daemon=True).start()
        
    def process_structure(self):
        src = self.paths[0].get()
        dst = self.paths[1].get()
        
        if not src or not dst:
            self.log("Error: Please set both source and destination paths")
            return
            
        self.stats["done"] = 0
        total = self.stats["files"] + self.stats["dirs"]
        
        for root, dirs, files in os.walk(src):
            rel_path = os.path.relpath(root, src)
            dest_path = os.path.join(dst, rel_path)
            
            try:
                os.makedirs(dest_path, exist_ok=True)
                self.stats["done"] += 1
                self.progress.set(self.stats["done"] / total)
                
                for file in files:
                    open(os.path.join(dest_path, file), "w").close()
                    self.stats["done"] += 1
                    self.progress.set(self.stats["done"] / total)
                    
            except Exception as e:
                self.log(f"Error: {e}")
                
        self.log("Operation completed successfully")
        self.execute_btn.configure(state="normal")
        
    def start_operation(self):
        self.progress.set(0)
        self.execute_btn.configure(state="disabled")
        threading.Thread(target=self.process_structure, daemon=True).start()

if __name__ == "__main__":
    app = GhostRetro()
    app.mainloop()
