import customtkinter as ctk
from tkinter import messagebox
from .mapping_editor import MappingEditor

class MappingEditorWindow(ctk.CTkToplevel):
    def __init__(self, parent, initial_data=None):
        super().__init__(parent)
        self.title("Configure Target Blueprint Asset Mapping")
        self.geometry("520x680")
        self.resizable(False, False)
        self.configure(fg_color="#0a0a0a")
        
        self.transient(parent)
        self.grab_set()
        
        self.saved_data = None
        self.columnconfigure(0, weight=1)
        self.rowconfigure(0, weight=1)

        # Wrap mapping panel into scroll container layout
        self.scroll_container = ctk.CTkScrollableFrame(self, fg_color="#0a0a0a")
        self.scroll_container.grid(row=0, column=0, sticky="nsew", padx=5, pady=5)
        self.scroll_container.columnconfigure(0, weight=1)

        self.editor = MappingEditor(self.scroll_container, initial_data=initial_data)
        self.editor.grid(row=0, column=0, sticky="nsew")

        # Action Control Buttons Tray Block 
        btn_frame = ctk.CTkFrame(self, fg_color="#111111", height=60, corner_radius=0)
        btn_frame.grid(row=1, column=0, sticky="ew")
        btn_frame.pack_propagate(False)

        save_btn = ctk.CTkButton(btn_frame, text="Save Mapping Changes", fg_color="#2e7d32", text_color="#ffffff", hover_color="#1b5e20", width=160, font=("Arial", 12, "bold"), command=self.save)
        save_btn.pack(side="right", padx=15, pady=12)

        cancel_btn = ctk.CTkButton(btn_frame, text="Discard Changes", fg_color="#242424", text_color="#d0d0d0", hover_color="#3a3a3a", width=130, command=self.destroy)
        cancel_btn.pack(side="right", pady=12)

        self.wait_window(self)

    def save(self):
        try:
            self.saved_data = self.editor.get_all_paths()
        except ValueError as error:
            messagebox.showerror("Invalid Mapping", str(error), parent=self)
            return
        self.destroy()
