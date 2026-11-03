import customtkinter as ctk
from tkinter import ttk, messagebox
from pathlib import Path

from pipeline import Pipeline
from pipeline.mapping_manager import MappingManager
from .validation_dialog import MappingEditorWindow

class MainWindow(ctk.CTk):
    def __init__(self, config_path=None):
        super().__init__()
        ctk.set_appearance_mode("Dark")

        self.config_path = Path(config_path or Path(__file__).resolve().parents[2] / "config" / "pipeline.json")
        self.mapping_manager = MappingManager(self.config_path)
        self.title("Multi-Asset Cooking Suite")
        self.geometry("900x550")
        self.configure(fg_color="#000000")
        self.mappings_dataset = self.mapping_manager.load_mappings()

        self._setup_window_grid()
        self._build_header_pane()
        self._build_tabular_view()
        self._build_control_tray()
        self._refresh_table_records()

    def _setup_window_grid(self):
        self.columnconfigure(0, weight=1)
        self.rowconfigure(1, weight=1) # Tabular data canvas grows vertically

    def _build_header_pane(self):
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.grid(row=0, column=0, sticky="ew", padx=20, pady=(15, 10))
        header.columnconfigure(0, weight=1)

        title = ctk.CTkLabel(header, text="Asset Mapping Pipelines", font=("Arial", 22, "bold"), text_color="#ffffff", anchor="w")
        title.grid(row=0, column=0, sticky="w")
        
        add_btn = ctk.CTkButton(header, text="+ Add New Asset Mapping", fg_color="#1c1c1c", hover_color="#2c2c2c", font=("Arial", 12, "bold"), height=35, command=self._add_new_mapping_pipeline)
        add_btn.grid(row=0, column=1, sticky="e")

    def _build_tabular_view(self):
        # Configure deep contextual system table styling theme mappings globally
        style = ttk.Style()
        style.theme_use("default")
        style.configure("Treeview", background="#0a0a0a", fieldbackground="#0a0a0a", foreground="#d0d0d0", borderwidth=0, font=("Arial", 11), rowheight=32)
        style.configure("Treeview.Heading", background="#141414", foreground="#ffffff", borderwidth=0, font=("Arial", 11, "bold"))
        
        # Row selection highlighters map layout profiles cleanly
        style.map("Treeview", background=[("selected", "#1b5e20")], foreground=[("selected", "#ffffff")])
        style.map("Treeview.Heading", background=[("active", "#1c1c1c")])

        # Table display initialization configuration
        columns = ("id", "stub_bp", "unreal_pkg", "overwrite")
        self.tree = ttk.Treeview(self, columns=columns, show="headings", selectmode="browse")
        
        self.tree.heading("id", text="Pipeline ID", anchor="w")
        self.tree.heading("stub_bp", text="Stub Blueprint Package Path Target", anchor="w")
        self.tree.heading("unreal_pkg", text="Target Unreal Package Base Path", anchor="w")
        self.tree.heading("overwrite", text="Post-Cook Overwrite Status", anchor="center")

        self.tree.column("id", width=100, minwidth=80, stretch=False)
        self.tree.column("stub_bp", width=340, minwidth=200, stretch=True)
        self.tree.column("unreal_pkg", width=280, minwidth=180, stretch=True)
        self.tree.column("overwrite", width=140, minwidth=120, stretch=False, anchor="center")

        self.tree.grid(row=1, column=0, sticky="nsew", padx=20, pady=10)
        
        # --- Event Bindings: Hook up Double-Click Editing Action ---
        self.tree.bind("<Double-1>", lambda event: self._edit_selected_mapping_pipeline())

    def _build_control_tray(self):
        tray = ctk.CTkFrame(self, fg_color="transparent")
        tray.grid(row=2, column=0, sticky="ew", padx=20, pady=(10, 20))
        
        edit_btn = ctk.CTkButton(tray, text="Edit Definition Path Settings", fg_color="#242424", hover_color="#323232", text_color="#ffffff", height=38, command=self._edit_selected_mapping_pipeline)
        edit_btn.pack(side="left", padx=(0, 10))

        delete_btn = ctk.CTkButton(tray, text="Remove Pipeline Entry", fg_color="#3e1c1c", hover_color="#5c2424", text_color="#ff8888", height=38, command=self._delete_selected_mapping_pipeline)
        delete_btn.pack(side="left")

        execute_btn = ctk.CTkButton(tray, text="Execute All Active Cook Implementations", fg_color="#2e7d32", hover_color="#1b5e20", text_color="#ffffff", font=("Arial", 12, "bold"), height=38, command=self.execute_baking_pipeline)
        execute_btn.pack(side="right")

    def _refresh_table_records(self):
        # Flush existing table rows clean 
        for row in self.tree.get_children():
            self.tree.delete(row)
            
        # Re-populate data matrix items array seamlessly
        for uid, dataset in self.mappings_dataset.items():
            overwrite_lbl = "Enabled" if dataset.get("overwrite_after_cook") else "Disabled"
            self.tree.insert("", "end", iid=uid, values=(uid, dataset.get("stub_blueprint"), dataset.get("unreal_package"), overwrite_lbl))

    def _add_new_mapping_pipeline(self):
        next_index = 1
        while f"ID_{next_index:03d}" in self.mappings_dataset:
            next_index += 1
        new_uid = f"ID_{next_index:03d}"
        
        editor_popup = MappingEditorWindow(self, initial_data=None)
        if editor_popup.saved_data:
            self.mappings_dataset[new_uid] = editor_popup.saved_data
            self._save_mappings()
            self._refresh_table_records()

    def _edit_selected_mapping_pipeline(self):
        selected_item = self.tree.selection()
        if not selected_item:
            messagebox.showwarning("Selection Missing", "Please select a mapping definition row to modify.")
            return
            
        target_uid = selected_item[0]
        active_config_data = self.mappings_dataset[target_uid]
        
        # Fire transient parameter editor panel modal wrapper directly passing baseline dictionary
        editor_popup = MappingEditorWindow(self, initial_data=active_config_data)
        if editor_popup.saved_data:
            self.mappings_dataset[target_uid] = editor_popup.saved_data
            self._save_mappings()
            self._refresh_table_records()

    def _delete_selected_mapping_pipeline(self):
        selected_item = self.tree.selection()
        if not selected_item:
            return
        target_uid = selected_item[0]
        if messagebox.askyesno("Confirm Removal", f"Permanently drop tracking profile configuration record: {target_uid}?"):
            del self.mappings_dataset[target_uid]
            self._save_mappings()
            self._refresh_table_records()

    def _save_mappings(self):
        self.mapping_manager.save_mappings(self.mappings_dataset)

    def execute_baking_pipeline(self):
        total_pipelines = len(self.mappings_dataset)
        if total_pipelines == 0:
            messagebox.showwarning("Pipeline Empty", "No active asset tracking definitions are currently present to execute.")
            return

        try:
            self._save_mappings()
            output = Pipeline(self.config_path).execute()
        except Exception as error:
            messagebox.showerror("Pipeline Failed", str(error))
            return
        messagebox.showinfo(
            "Automation Sequence Complete",
            f"Built {total_pipelines} asset mapping(s).\nPAK: {output}",
        )

if __name__ == "__main__":
    app = MainWindow()
    app.mainloop()
