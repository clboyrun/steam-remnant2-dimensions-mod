import json
import customtkinter as ctk
from tkinter import filedialog
import os

class PathSelectorWidget(ctk.CTkFrame):
    """A composite widget containing a label, an entry field, and an optional browse button."""
    def __init__(self, parent, label_text, default_value, browse_type="file", file_types=None, **kwargs):
        super().__init__(parent, fg_color="transparent", **kwargs)
        self.columnconfigure(0, weight=1)
        
        self.browse_type = browse_type  # "file", "directory", or "none"
        self.file_types = file_types or [("All Files", "*.*")]

        # Sub-heading field label
        sub_lbl = ctk.CTkLabel(
            self, text=label_text, font=("Arial", 11, "bold"), 
            text_color="#b0b0b0", anchor="w"
        )
        sub_lbl.grid(row=0, column=0, columnspan=2, sticky="ew", pady=(4, 2))

        # Core Entry Box
        self.entry = ctk.CTkEntry(
            self, height=35, corner_radius=6, 
            fg_color="#141414", border_color="#242424", 
            text_color="#a5a5a5", font=("Consolas", 11)
        )
        if default_value:
            self.entry.insert(0, default_value)
        
        if self.browse_type != "none":
            self.entry.grid(row=1, column=0, sticky="ew", padx=(0, 6))
            
            # Interactive Browse Button
            self.browse_btn = ctk.CTkButton(
                self, text="Browse...", width=80, height=35, corner_radius=6,
                fg_color="#1c1c1c", hover_color="#2a2a2a", text_color="#ffffff",
                font=("Arial", 11, "bold"), command=self._handle_browse
            )
            self.browse_btn.grid(row=1, column=1, sticky="e")
        else:
            self.entry.grid(row=1, column=0, columnspan=2, sticky="ew")

    def _handle_browse(self):
        initial_dir = os.path.dirname(self.entry.get()) if os.path.exists(os.path.dirname(self.entry.get())) else None
        
        if self.browse_type == "file":
            selected_path = filedialog.askopenfilename(
                title="Select Asset File",
                initialdir=initial_dir,
                filetypes=self.file_types
            )
        elif self.browse_type == "directory":
            selected_path = filedialog.askdirectory(
                title="Select Workspace Directory",
                initialdir=initial_dir
            )
        else:
            return

        if selected_path:
            # Normalize slashes cleanly for multi-platform developer environments
            clean_path = selected_path.replace("\\", "/")
            self.entry.delete(0, "end")
            self.entry.insert(0, clean_path)

    def get(self):
        return self.entry.get()


class MappingEditor(ctk.CTkFrame):
    def __init__(self, parent, initial_data=None, **kwargs):
        super().__init__(parent, fg_color="#0a0a0a", **kwargs)
        self.columnconfigure(0, weight=1)
        self.initial_data = initial_data or {}
        self._build_ui()
        
    def _build_ui(self):
        # --- Section Title Block ---
        title_lbl = ctk.CTkLabel(self, text="Asset Mapping Editor", font=("Arial", 18, "bold"), text_color="#ffffff", anchor="w")
        title_lbl.grid(row=0, column=0, sticky="ew", padx=15, pady=(15, 2))
        
        # Extension filters for file dialogue operations
        uasset_filter = [("Unreal Asset Files", "*.uasset"), ("All Files", "*.*")]
        uexp_filter = [("Unreal Export Files", "*.uexp"), ("All Files", "*.*")]

        # --- Section 1: Original Asset ---
        self._add_section_header("Original asset", 2)
        self.orig_uasset = self._add_path_selector("Original UAsset", "original_uasset", "D:/FModel/Export/Example.uasset", 3, "file", uasset_filter)
        self.orig_uexp = self._add_path_selector("Original UEXP", "original_uexp", "D:/FModel/Export/Example.uexp", 5, "file", uexp_filter)

        # --- Section 2: ORIG Asset ---
        self._add_section_header("ORIG asset", 7)
        self.exported_uasset = self._add_path_selector("Exported UAsset", "exported_uasset", "D:/Mod/Workspace/ORIG/Example_ORIG.uasset", 8, "file", uasset_filter)
        self.exported_uexp = self._add_path_selector("Exported UEXP", "exported_uexp", "D:/Mod/Workspace/ORIG/Example_ORIG.uexp", 9, "file", uexp_filter)
        # Note: Unreal packages are virtual system strings (/Game/...), browse is deactivated to preserve typing context
        self.unreal_package = self._add_path_selector("Unreal package path", "unreal_package", "/Game/Originals/Example_ORIG", 11, "none")

        # --- Section 3: Blueprint Hierarchy ---
        self._add_section_header("Blueprint hierarchy", 13)
        self.stub_blueprint = self._add_path_selector("Stub Blueprint package", "stub_blueprint", "/Game/Generated/Example_Stub", 14, "none")
        self.custom_blueprint = self._add_path_selector("Custom Blueprint package", "custom_blueprint", "/Game/Mods/Example", 16, "none")
        self.parent_class = self._add_path_selector("Parent class", "parent_class", "/Game/Originals/Example_ORIG.Example_ORIG_C", 18, "none")

        # --- Section 4: Blueprint components ---
        self._add_section_header("Stub Blueprint components (JSON)", 20)
        components_label = ctk.CTkLabel(
            self,
            text='[{"name":"Mesh","type":"StaticMeshComponent","parent":"DefaultSceneRoot"}]',
            font=("Arial", 10),
            text_color="#909090",
            anchor="w",
        )
        components_label.grid(row=21, column=0, sticky="ew", padx=15, pady=(0, 3))
        self.components_entry = ctk.CTkTextbox(
            self,
            height=64,
            fg_color="#141414",
            border_color="#242424",
            text_color="#a5a5a5",
            font=("Consolas", 10),
        )
        self.components_entry.grid(row=22, column=0, sticky="ew", padx=15, pady=(2, 6))
        self.components_entry.insert(
            "1.0",
            json.dumps(self.initial_data.get("components", []), indent=2),
        )

        # --- Section 5: Post-Bake Replacement ---
        self._add_section_header("Post-bake replacement", 23)
        self.cooked_destination = self._add_path_selector("Cooked destination UAsset", "cooked_destination", "D:/Mod/Cooked/ExactPath/Example.uasset", 24, "file", uasset_filter)
        self.replacement_source = self._add_path_selector("Replacement source UAsset", "replacement_source", "D:/Mod/Workspace/ORIG/Example_ORIG.uasset", 26, "file", uasset_filter)
        self.replacement_source_uexp = self._add_path_selector("Replacement source UEXP", "replacement_source_uexp", "D:/Mod/Workspace/ORIG/Example_ORIG.uexp", 28, "file", uexp_filter)

        # --- Toggle Footer Row ---
        self._build_toggle_footer(30)

    def _add_section_header(self, text, start_row):
        lbl = ctk.CTkLabel(self, text=text, font=("Arial", 13, "bold"), text_color="#ffffff", anchor="w")
        lbl.grid(row=start_row, column=0, sticky="ew", padx=15, pady=(12, 4))

    def _add_path_selector(self, label_text, data_key, fallback_placeholder, start_row, browse_type="file", file_types=None):
        val = self.initial_data.get(data_key, fallback_placeholder)
        
        selector = PathSelectorWidget(self, label_text=label_text, default_value=val, browse_type=browse_type, file_types=file_types)
        selector.grid(row=start_row, column=0, sticky="ew", padx=15, pady=(2, 6))
        return selector

    def _build_toggle_footer(self, row):
        footer_frame = ctk.CTkFrame(self, fg_color="transparent")
        footer_frame.grid(row=row, column=0, sticky="ew", padx=15, pady=(15, 15))
        footer_frame.columnconfigure(0, weight=1)

        lbl = ctk.CTkLabel(footer_frame, text="Overwrite after cook", font=("Arial", 12, "bold"), text_color="#ffffff", anchor="w")
        lbl.grid(row=0, column=0, sticky="w")

        init_status = "on" if self.initial_data.get("overwrite_after_cook", True) else "off"
        self.overwrite_var = ctk.StringVar(value=init_status)
        
        text_state = "Enabled" if init_status == "on" else "Disabled"
        text_color = "#2e7d32" if init_status == "on" else "#757575"
        
        self.toggle = ctk.CTkSwitch(
            footer_frame, text=text_state, font=("Arial", 11, "bold"),
            text_color=text_color, fg_color="#1c1c1c", progress_color="#2e7d32",
            variable=self.overwrite_var, onvalue="on", offvalue="off",
            command=self._on_toggle_changed
        )
        self.toggle.grid(row=0, column=1, sticky="e")

    def _on_toggle_changed(self):
        if self.overwrite_var.get() == "on":
            self.toggle.configure(text_color="#2e7d32", text="Enabled")
        else:
            self.toggle.configure(text_color="#757575", text="Disabled")

    def get_all_paths(self):
        components = json.loads(self.components_entry.get("1.0", "end").strip() or "[]")
        if not isinstance(components, list):
            raise ValueError("Stub Blueprint components must be a JSON list.")
        return {
            "original_uasset": self.orig_uasset.get(),
            "original_uexp": self.orig_uexp.get(),
            "exported_uasset": self.exported_uasset.get(),
            "exported_uexp": self.exported_uexp.get(),
            "unreal_package": self.unreal_package.get(),
            "stub_blueprint": self.stub_blueprint.get(),
            "custom_blueprint": self.custom_blueprint.get(),
            "parent_class": self.parent_class.get(),
            "cooked_destination": self.cooked_destination.get(),
            "replacement_source": self.replacement_source.get(),
            "replacement_source_uexp": self.replacement_source_uexp.get(),
            "overwrite_after_cook": self.overwrite_var.get() == "on",
            "components": components,
            "replace_extensions": self.initial_data.get(
                "replace_extensions", [".uasset", ".uexp"]
            ),
        }
