import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import os
import ast
import subprocess
import shutil

CONFIG_FILE = "config.txt"
TAB_KEYS = {
    "yaml": "YAML",
    "single": "Single ad",
    "diss": "Disassemble",
    "quick": "Quick Build",
    "setting": "Settings"
}

def parse_config(filepath):
    config = {}
    if not os.path.exists(filepath):
        return config

    def parse_array(val: str):
        items = []
        i = 0
        n = len(val)
        while i < n:
            if val[i] == '"':
                i += 1
                start = i
                while i < n and val[i] != '"':
                    if val[i] == '\\' and i + 1 < n:  # Skip escaped quotes like \" or \\
                        i += 2
                    else:
                        i += 1
                items.append(val[start:i])
                i += 1  # skip ending quote
            else:
                i += 1  # skip commas or spaces
        return items

    with open(filepath, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith('//'):
                continue
            if '=' not in line:
                continue

            key, val = line.split('=', 1)
            key = key.strip()
            val = val.strip()

            if val.startswith('[') and val.endswith(']'):
                config[key] = parse_array(val)
            elif val.lower() in ('true', 'false'):
                config[key] = val.lower() == 'true'
            else:
                config[key] = val.strip('"').strip("'")

    return config

class QuickBuildTab(ttk.Frame):
    def __init__(self, parent, config, config_path):
        super().__init__(parent)
        self.quick_build_entries = []
        self.auto_diss_var = tk.BooleanVar(value=config.get("AUTO_DISS_ON_QUICKBUILD", False))
        self.config_path = config_path
        self._load_from_config(config)
        self._build_ui()

    def _load_from_config(self, config):
        i = 0
        self.quick_build_entries.clear()
        while True:
            key = f"QUICK_BUILD_LIST_{i}"
            if key not in config:
                break
            entry = config[key]
            if isinstance(entry, list) and len(entry) == 6:
                self.quick_build_entries.append({
                    "label": entry[0],
                    "mode": entry[1],
                    "ad_input": entry[2],
                    "version": entry[3],
                    "yaml_input": entry[4],
                    "output_adc": entry[5]
                })
            i += 1

    def _build_ui(self):
        top_frame = ttk.Frame(self)
        top_frame.pack(fill="x", pady=5, padx=5)

        auto_diss_check = ttk.Checkbutton(top_frame, text="Auto Disassemble on Build", variable=self.auto_diss_var)
        auto_diss_check.pack(side="left")

        add_button = ttk.Button(top_frame, text="Add Quick Build", command=self._add_dummy_entry)
        add_button.pack(side="right")

        self.list_frame = ttk.Frame(self)
        self.list_frame.pack(fill="both", expand=True, padx=10, pady=5)

        self._refresh_list()

    def _add_dummy_entry(self):
        new_entry = {
            "label": f"Script {len(self.quick_build_entries)+1}",
            "mode": "YAML",
            "ad_input": "",
            "version": "",
            "yaml_input": "",
            "output_adc": ""
        }
        self.quick_build_entries.append(new_entry)
        self._refresh_list()
        self._open_config(len(self.quick_build_entries) - 1)
        self.save_to_config()
        
    def _delete_entry(self, index):
        confirm = messagebox.askyesno("Delete Quick Build", f"Delete '{self.quick_build_entries[index]['label']}'?")
        if confirm:
            del self.quick_build_entries[index]
            self._refresh_list()
            self.save_to_config()

    def _refresh_list(self):
        for child in self.list_frame.winfo_children():
            child.destroy()
    
        for index, entry in enumerate(self.quick_build_entries):
            row = ttk.Frame(self.list_frame)
            row.pack(fill="x", pady=2)
    
            label = ttk.Label(row, text=entry["label"], width=30)
            label.pack(side="left", padx=2)
    
            up_btn = ttk.Button(row, text="↑", width=2, command=lambda i=index: self._move_entry(i, -1))
            up_btn.pack(side="left", padx=2)
    
            down_btn = ttk.Button(row, text="↓", width=2, command=lambda i=index: self._move_entry(i, 1))
            down_btn.pack(side="left", padx=2)
    
            run_btn = ttk.Button(row, text="Build", width=50, command=lambda i=index: self._run_entry(i))
            run_btn.pack(side="left", padx=2)
    
            config_btn = ttk.Button(row, text="Configure", command=lambda i=index: self._open_config(i))
            config_btn.pack(side="left", padx=2)
    
            delete_btn = ttk.Button(row, text="Delete", command=lambda i=index: self._delete_entry(i))
            delete_btn.pack(side="left", padx=2)

    def _move_entry(self, index, direction):
        new_index = index + direction
        if 0 <= new_index < len(self.quick_build_entries):
            self.quick_build_entries[index], self.quick_build_entries[new_index] = \
                self.quick_build_entries[new_index], self.quick_build_entries[index]
            self._refresh_list()
            self.save_to_config()

    def _run_entry(self, index):
        entry = self.quick_build_entries[index]
    
        mode = entry["mode"]
        yaml_input = entry["yaml_input"]
        ad_input = entry["ad_input"]
        output_adc = entry["output_adc"]
        version = entry["version"]
    
        # Read full config to get ADHOC_DIR and AUTO_DISS_ON_QUICKBUILD
        config = parse_config(self.config_path)
        adhoc_path = config.get("ADHOC_DIR", "")
        auto_diss = config.get("AUTO_DISS_ON_QUICKBUILD", False)
    
        if not os.path.isfile(adhoc_path):
            messagebox.showerror("Error", f"adhoc.exe not found at:\n{adhoc_path}")
            return
    
        args = [adhoc_path, "build"]
    
        if mode == "YAML":
            if not yaml_input or not output_adc:
                messagebox.showwarning("Missing Input", "YAML input or output path is missing.")
                return
            args += ["-i", yaml_input, "-o", output_adc]
    
        elif mode == "SINGLE":
            if not ad_input or not output_adc or not version:
                messagebox.showwarning("Missing Input", "Single build requires .ad input, output path, and version.")
                return
            args += ["-i", ad_input, "-o", output_adc, "-v", version]
    
        else:
            messagebox.showerror("Error", f"Unknown build mode: {mode}")
            return
    
        print(f"[Run] Executing: {' '.join(args)}")
    
        try:
            subprocess.run(args, check=True)
    
            if auto_diss:
                print(f"[Run] Auto-disassemble: {adhoc_path} {output_adc}")
                subprocess.run([adhoc_path, output_adc], check=True)
    
            #messagebox.showinfo("Success", f"Build complete for: {entry['label']}")
        except subprocess.CalledProcessError as e:
            messagebox.showerror("Build Failed", f"Build failed:    \n{e}")

    def _open_config(self, index):
        entry = self.quick_build_entries[index]
    
        win = tk.Toplevel(self)
        win.title(f"Configure: {entry['label']}")
        win.geometry("500x450")
        win.grab_set()
    
        mode_var = tk.StringVar(value=entry['mode'])
    
        label_var = tk.StringVar(value=entry['label'])
        ad_input_var = tk.StringVar(value=entry['ad_input'])
        version_var = tk.StringVar(value=entry['version'])
        yaml_input_var = tk.StringVar(value=entry['yaml_input'])
        output_adc_var = tk.StringVar(value=entry['output_adc'])
    
        def update_fields():
            is_yaml = (mode_var.get() == "YAML")
        
            if is_yaml:
                ad_input_var.set("")
                version_var.set("")
                version_dropdown_var.set("Defined in YAML")
                version_combo.config(state="disabled")
            else:
                yaml_input_var.set("")
                ad_input_entry.config(state="normal")
                ad_browse_btn.config(state="normal")
        
                # Only reset version if it was empty (i.e. just came from YAML)
                if not version_var.get():
                    version_var.set("5")
                    version_dropdown_var.set("Version 5 (GT4P / Retail GT4)")
                else:
                    version_dropdown_var.set(REVERSE_VERSION_MAP.get(version_var.get(), "Version 5 (GT4P / Retail GT4)"))
        
                version_combo.config(state="readonly")
        
            ad_input_entry.config(state="normal" if not is_yaml else "disabled")
            yaml_input_entry.config(state="normal" if is_yaml else "disabled")
            ad_browse_btn.config(state="normal" if not is_yaml else "disabled")
            yaml_browse_btn.config(state="normal" if is_yaml else "disabled")
    
        def browse_file(var):
            path = filedialog.askopenfilename(title="Select File")
            if path:
                var.set(path)
    
        # --- Build Mode ---
        mode_frame = ttk.LabelFrame(win, text="Build Mode")
        mode_frame.pack(fill="x", padx=10, pady=10)
    
        ttk.Radiobutton(mode_frame, text="YAML", variable=mode_var, value="YAML", command=update_fields).pack(anchor="w", padx=5)
        ttk.Radiobutton(mode_frame, text="Single .ad", variable=mode_var, value="SINGLE", command=update_fields).pack(anchor="w", padx=5)
    
        # --- Input .ad ---
        ttk.Label(win, text="Input .ad:").pack(anchor="w", padx=10)
        ad_row = ttk.Frame(win)
        ad_row.pack(fill="x", padx=10, pady=2)
        ad_input_entry = ttk.Entry(ad_row, textvariable=ad_input_var)
        ad_input_entry.pack(side="left", fill="x", expand=True)
        ad_browse_btn = ttk.Button(ad_row, text="Browse", command=lambda: browse_file(ad_input_var))
        ad_browse_btn.pack(side="left", padx=5)
    
        # --- Adhoc Version ---
        ttk.Label(win, text="Adhoc Version:").pack(anchor="w", padx=10)
        
        VERSION_MAP = {
            "Version 5 (GT4P / Retail GT4)": "5",
            "Version 7 (GT4O / TT)": "7",
            "Version 10 (GTHD / GT5P)": "10",
            "Version 12 (GTPSP, GT5, GT6, GT Sport)": "12",
            "Defined in YAML": "yaml"
        }
        REVERSE_VERSION_MAP = {v: k for k, v in VERSION_MAP.items()}
        
        # Default to 5 if version is not known or if switching back from YAML
        actual_version = version_var.get() if version_var.get() in REVERSE_VERSION_MAP else "5"
        version_dropdown_var = tk.StringVar(value=REVERSE_VERSION_MAP.get(actual_version, "Version 5 (GT4P / Retail GT4)"))
        
        def update_version_var(*args):
            if version_dropdown_var.get() == "Defined in YAML":
                version_var.set("")
            else:
                version_var.set(VERSION_MAP.get(version_dropdown_var.get(), ""))
        
        version_dropdown_var.trace_add("write", update_version_var)
        
        version_combo = ttk.Combobox(win, textvariable=version_dropdown_var, state="readonly")
        version_combo["values"] = [label for label in VERSION_MAP if label != "Defined in YAML"]
        version_combo.pack(fill="x", padx=10, pady=2)
    
        # --- Input YAML ---
        ttk.Label(win, text="Input YAML:").pack(anchor="w", padx=10)
        yaml_row = ttk.Frame(win)
        yaml_row.pack(fill="x", padx=10, pady=2)
        yaml_input_entry = ttk.Entry(yaml_row, textvariable=yaml_input_var)
        yaml_input_entry.pack(side="left", fill="x", expand=True)
        yaml_browse_btn = ttk.Button(yaml_row, text="Browse", command=lambda: browse_file(yaml_input_var))
        yaml_browse_btn.pack(side="left", padx=5)
    
        # --- Output .adc ---
        ttk.Label(win, text="Output .adc:").pack(anchor="w", padx=10)
        out_row = ttk.Frame(win)
        out_row.pack(fill="x", padx=10, pady=2)
        output_adc_entry = ttk.Entry(out_row, textvariable=output_adc_var)
        output_adc_entry.pack(side="left", fill="x", expand=True)
        out_browse_btn = ttk.Button(out_row, text="Browse", command=lambda: browse_file(output_adc_var))
        out_browse_btn.pack(side="left", padx=5)
    
        # --- Label ---
        ttk.Label(win, text="Button Label:").pack(anchor="w", padx=10)
        label_entry = ttk.Entry(win, textvariable=label_var)
        label_entry.pack(fill="x", padx=10, pady=2)
    
        # --- Save Button ---
        def save_config():
            entry["label"] = label_var.get()
            entry["mode"] = mode_var.get()
            entry["ad_input"] = ad_input_var.get()
            entry["version"] = version_var.get()
            entry["yaml_input"] = yaml_input_var.get()
            entry["output_adc"] = output_adc_var.get()
            self._refresh_list()
            self.save_to_config()
            win.destroy()
    
        ttk.Button(win, text="Save", command=save_config).pack(pady=15)
    
        update_fields()

    def save_to_config(self):
        try:
            with open(self.config_path, "r", encoding="utf-8") as f:
                lines = f.readlines()
        except FileNotFoundError:
            lines = []
    
        # Strip out any existing QUICK_BUILD_LIST_x lines
        lines = [line for line in lines if not line.strip().startswith("QUICK_BUILD_LIST_")]
    
        # Re-add updated quick build list
        for i, entry in enumerate(self.quick_build_entries):
            quoted_list = [
                f'"{entry["label"]}"',
                f'"{entry["mode"]}"',
                f'"{entry["ad_input"]}"',
                f'"{entry["version"]}"',
                f'"{entry["yaml_input"]}"',
                f'"{entry["output_adc"]}"'
            ]
            line = f'QUICK_BUILD_LIST_{i} = [{", ".join(quoted_list)}]\n'
            lines.append(line)
    
        # Add/replace AUTO_DISS_ON_QUICKBUILD
        lines = [line for line in lines if not line.strip().startswith("AUTO_DISS_ON_QUICKBUILD")]
        lines.append(f"AUTO_DISS_ON_QUICKBUILD = {'true' if self.auto_diss_var.get() else 'false'}\n")
    
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                f.writelines(lines)
            print("[Config] Saved successfully.")
        except Exception as e:
            print(f"[Config] Failed to save: {e}")
            
class CommandLineWrapperApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Adhoc Toolchain GUI Wrapper")
        self.geometry("750x550")
        self.config_path = CONFIG_FILE
        self.config_data = parse_config(self.config_path)
        
        # First-time boot logic
        if not os.path.exists(self.config_path) or not self.config_data or "ADHOC_DIR" not in self.config_data:
            self._first_time_setup()

        self.create_tabs()

        # Select default tab
        tab_key = self.config_data.get("DEFAULT_TAB", "quick").lower()
        tab_name = TAB_KEYS.get(tab_key, "Quick Build")
        if tab_name in TAB_KEYS.values():
            idx = list(TAB_KEYS.values()).index(tab_name)
            self.tab_control.select(idx)
        
    def _first_time_setup(self):
        messagebox.showinfo("First-time Setup", "Adhoc Toolchain location not specified. Press OK to locate adhoc.exe on your system")
    
        exe_path = filedialog.askopenfilename(
            title="Select adhoc.exe",
            filetypes=[("Executable", "*.exe")],
        )
    
        if not exe_path:
            messagebox.showerror("Setup Incomplete", "adhoc.exe path was not selected. Exiting.")
            self.destroy()
            return
    
        self.config_data["ADHOC_DIR"] = exe_path
        self._write_config(self.config_data)
        print("[Setup] Saved ADHOC_DIR:", exe_path)
        
    def _write_config(self, config_dict):
        lines = []
    
        # Save ADHOC_DIR
        if "ADHOC_DIR" in config_dict:
            lines.append(f'ADHOC_DIR = "{config_dict["ADHOC_DIR"]}"\n')
    
        # Preserve DEFAULT_TAB if present
        if "DEFAULT_TAB" in config_dict:
            lines.append(f'DEFAULT_TAB = {config_dict["DEFAULT_TAB"]}\n')
    
        # Preserve AUTO_DISS_ON_QUICKBUILD
        if "AUTO_DISS_ON_QUICKBUILD" in config_dict:
            lines.append(f'AUTO_DISS_ON_QUICKBUILD = {"true" if config_dict["AUTO_DISS_ON_QUICKBUILD"] else "false"}\n')
    
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                f.writelines(lines)
        except Exception as e:
            messagebox.showerror("Write Failed", f"Failed to save config.txt:\n{e}")

    def create_tabs(self):
        self.tab_control = ttk.Notebook(self)
        self.tab_control.pack(expand=True, fill="both")

        # Tab 1: YAML
        self.yaml_tab = ttk.Frame(self.tab_control)
        self.tab_control.add(self.yaml_tab, text="YAML")
        self._populate_yaml_tab()

        # Tab 2: Single ad
        self.single_ad_tab = ttk.Frame(self.tab_control)
        self.tab_control.add(self.single_ad_tab, text="Single ad")
        self._populate_single_ad_tab()

        # Tab 3: Disassemble
        self.disassemble_tab = ttk.Frame(self.tab_control)
        self.tab_control.add(self.disassemble_tab, text="Disassemble")
        self._populate_disassemble_tab()

        # Tab 4: Quick Build
        self.quick_build_tab = ttk.Frame(self.tab_control)
        self.tab_control.add(self.quick_build_tab, text="Quick Build")
        self._populate_quick_build_tab()

        # Tab 5: Settings
        self.settings_tab = ttk.Frame(self.tab_control)
        self.tab_control.add(self.settings_tab, text="Settings")
        self._populate_settings_tab()

    def _populate_yaml_tab(self):
        frame = ttk.Frame(self.yaml_tab, padding=10)
        frame.pack(fill="both", expand=True)
    
        # Variables
        self.yaml_input_var = tk.StringVar()
        self.yaml_output_var = tk.StringVar()
    
        # Input YAML
        ttk.Label(frame, text="Input .yaml:").grid(row=0, column=0, sticky="w")
        input_row = ttk.Frame(frame)
        input_row.grid(row=1, column=0, columnspan=2, sticky="ew", pady=2)
        input_entry = ttk.Entry(input_row, textvariable=self.yaml_input_var)
        input_entry.pack(side="left", fill="x", expand=True)
        ttk.Button(input_row, text="Browse", command=lambda: self._browse_file(self.yaml_input_var, [("YAML files", "*.yaml")])).pack(side="left", padx=5)
    
        # Output .adc
        ttk.Label(frame, text="Output .adc:").grid(row=2, column=0, sticky="w", pady=(10, 0))
        output_row = ttk.Frame(frame)
        output_row.grid(row=3, column=0, columnspan=2, sticky="ew", pady=2)
        output_entry = ttk.Entry(output_row, textvariable=self.yaml_output_var)
        output_entry.pack(side="left", fill="x", expand=True)
        ttk.Button(output_row, text="Browse", command=lambda: self._browse_file(self.yaml_output_var, [("ADC files", "*.adc")], save=True)).pack(side="left", padx=5)
    
        # Run button
        ttk.Button(frame, text="Run", command=self._run_yaml).grid(row=4, column=0, pady=20, sticky="w")

    def _populate_single_ad_tab(self):
        frame = ttk.Frame(self.single_ad_tab, padding=10)
        frame.pack(fill="both", expand=True)
    
        # Variables
        self.single_ad_input_var = tk.StringVar()
        self.single_ad_output_var = tk.StringVar()
        self.single_ad_version_var = tk.StringVar()
    
        # Input .ad
        ttk.Label(frame, text="Input .ad:").grid(row=0, column=0, sticky="w")
        input_row = ttk.Frame(frame)
        input_row.grid(row=1, column=0, columnspan=2, sticky="ew", pady=2)
        input_entry = ttk.Entry(input_row, textvariable=self.single_ad_input_var)
        input_entry.pack(side="left", fill="x", expand=True)
        ttk.Button(input_row, text="Browse", command=lambda: self._browse_file(self.single_ad_input_var, [("AD files", "*.ad")])).pack(side="left", padx=5)
    
        # Output .adc
        ttk.Label(frame, text="Output .adc:").grid(row=2, column=0, sticky="w", pady=(10, 0))
        output_row = ttk.Frame(frame)
        output_row.grid(row=3, column=0, columnspan=2, sticky="ew", pady=2)
        output_entry = ttk.Entry(output_row, textvariable=self.single_ad_output_var)
        output_entry.pack(side="left", fill="x", expand=True)
        ttk.Button(output_row, text="Browse", command=lambda: self._browse_file(self.single_ad_output_var, [("ADC files", "*.adc")], save=True)).pack(side="left", padx=5)
    
        # Version dropdown
        ttk.Label(frame, text="Adhoc Version:").grid(row=4, column=0, sticky="w", pady=(10, 0))
    
        version_map = {
            "Version 5 (GT4P / Retail GT4)": "5",
            "Version 7 (GT4O / TT)": "7",
            "Version 10 (GTHD / GT5P)": "10",
            "Version 12 (GTPSP, GT5, GT6, GT Sport)": "12"
        }
        reverse_version_map = {v: k for k, v in version_map.items()}
    
        self.single_ad_version_dropdown_var = tk.StringVar(value=reverse_version_map.get("5"))
        version_dropdown = ttk.Combobox(frame, textvariable=self.single_ad_version_dropdown_var, state="readonly", width=50)
        version_dropdown["values"] = list(version_map.keys())
        version_dropdown.grid(row=5, column=0, sticky="ew", pady=2)
    
        def update_version_var(*args):
            self.single_ad_version_var.set(version_map.get(self.single_ad_version_dropdown_var.get(), ""))
    
        self.single_ad_version_dropdown_var.trace_add("write", update_version_var)
        update_version_var()  # Initialize
    
        # Run button
        ttk.Button(frame, text="Run", command=self._run_single_ad).grid(row=6, column=0, pady=20, sticky="w")

    def _populate_disassemble_tab(self):
        frame = ttk.Frame(self.disassemble_tab, padding=10)
        frame.pack(fill="both", expand=True)
    
        self.dis_input_var = tk.StringVar()
    
        # Input .adc
        ttk.Label(frame, text="Input .adc:").grid(row=0, column=0, sticky="w")
        input_row = ttk.Frame(frame)
        input_row.grid(row=1, column=0, columnspan=2, sticky="ew", pady=2)
        input_entry = ttk.Entry(input_row, textvariable=self.dis_input_var)
        input_entry.pack(side="left", fill="x", expand=True)
        ttk.Button(input_row, text="Browse", command=lambda: self._browse_file(self.dis_input_var, [("ADC files", "*.adc")])).pack(side="left", padx=5)
    
        # Run button
        ttk.Button(frame, text="Run", command=self._run_disassemble).grid(row=2, column=0, pady=20, sticky="w")

    def _populate_quick_build_tab(self):
        self.quick_build_widget = QuickBuildTab(self.quick_build_tab, self.config_data, self.config_path)
        self.quick_build_widget.pack(fill="both", expand=True)

    def _populate_settings_tab(self):
        frame = ttk.Frame(self.settings_tab, padding=20)
        frame.pack(fill="both", expand=True)
    
        # 1. Adhoc Toolchain Path
        ttk.Label(frame, text="Adhoc Toolchain Executable:").grid(row=0, column=0, sticky="w")
        self.adhoc_path_var = tk.StringVar(value=self.config_data.get("ADHOC_DIR", ""))
        path_row = ttk.Frame(frame)
        path_row.grid(row=1, column=0, columnspan=2, sticky="ew", pady=2)
        path_entry = ttk.Entry(path_row, textvariable=self.adhoc_path_var)
        path_entry.pack(side="left", fill="x", expand=True)
        ttk.Button(path_row, text="Browse", command=self._browse_adhoc_path).pack(side="left", padx=5)
    
        # 2. Default tab selection
        ttk.Label(frame, text="Default Tab on Startup:").grid(row=2, column=0, sticky="w", pady=(10, 0))
        tab_names = list(TAB_KEYS.values())
        self.default_tab_var = tk.StringVar(value=TAB_KEYS.get(self.config_data.get("DEFAULT_TAB", "yaml"), "YAML"))
        ttk.Combobox(frame, textvariable=self.default_tab_var, values=tab_names, state="readonly").grid(row=3, column=0, sticky="w")
    
        # 3. Credits box
        credits_text = (
            "Adhoc Toolchain GUI Wrapper by Silentwarior112\n"
            "Built for modding workflows\n"
        )
        ttk.Label(frame, text="Credits:").grid(row=4, column=0, sticky="w", pady=(20, 0))
        credits_box = tk.Text(frame, height=5, width=60, wrap="word")
        credits_box.grid(row=5, column=0, sticky="w")
        credits_box.insert("1.0", credits_text)
        credits_box.configure(state="disabled")
    
        # Save settings button
        ttk.Button(frame, text="Save Settings", command=self._save_settings).grid(row=6, column=0, pady=20, sticky="w")
        
    def _browse_adhoc_path(self):
        path = filedialog.askopenfilename(title="Locate adhoc.exe", filetypes=[("Executable", "*.exe")])
        if path:
            self.adhoc_path_var.set(path)
    
    def _save_settings(self):
        # Update config data
        self.config_data["ADHOC_DIR"] = self.adhoc_path_var.get()
        tab_key = [k for k, v in TAB_KEYS.items() if v == self.default_tab_var.get()]
        self.config_data["DEFAULT_TAB"] = tab_key[0] if tab_key else "yaml"
    
        # Rewrite config file
        try:
            with open(self.config_path, "r", encoding="utf-8") as f:
                lines = f.readlines()
        except FileNotFoundError:
            lines = []
    
        # Remove ADHOC_DIR and DEFAULT_TAB lines
        lines = [line for line in lines if not line.strip().startswith("ADHOC_DIR") and not line.strip().startswith("DEFAULT_TAB")]
    
        lines.append(f'ADHOC_DIR = "{self.config_data["ADHOC_DIR"]}"\n')
        lines.append(f'DEFAULT_TAB = {self.config_data["DEFAULT_TAB"]}\n')
    
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                f.writelines(lines)
            messagebox.showinfo("Settings Saved", "Settings have been saved successfully.")
        except Exception as e:
            messagebox.showerror("Error", f"Failed to save settings:\n{e}")
        
    def _browse_file(self, var, filetypes, save=False):
        path = filedialog.asksaveasfilename(filetypes=filetypes) if save else filedialog.askopenfilename(filetypes=filetypes)
        if path:
            var.set(path)

    def _run_yaml(self):
        config = parse_config(self.config_path)
        adhoc_path = config.get("ADHOC_DIR", "")
        yaml = self.yaml_input_var.get()
        out = self.yaml_output_var.get()
    
        if not os.path.isfile(adhoc_path):
            messagebox.showerror("Missing adhoc.exe", f"Adhoc executable not found:\n{adhoc_path}")
            return
    
        if not yaml or not out:
            messagebox.showwarning("Missing Paths", "Please specify both input .yaml and output .adc paths.")
            return
    
        args = [adhoc_path, "build", "-i", yaml, "-o", out]
        print("[YAML Run]", " ".join(args))
    
        try:
            subprocess.run(args, check=True)
            messagebox.showinfo("Success", "Build completed successfully.")
        except subprocess.CalledProcessError as e:
            messagebox.showerror("Build Failed", f"Build failed:\n{e}")
            
            
    def _run_single_ad(self):
        config = parse_config(self.config_path)
        adhoc_path = config.get("ADHOC_DIR", "")
    
        ad_input = self.single_ad_input_var.get()
        output = self.single_ad_output_var.get()
        version = self.single_ad_version_var.get()
    
        if not os.path.isfile(adhoc_path):
            messagebox.showerror("Missing adhoc.exe", f"Adhoc executable not found:\n{adhoc_path}")
            return
    
        if not ad_input or not output or not version:
            messagebox.showwarning("Missing Input", "Please specify .ad input, output path, and version.")
            return
    
        args = [adhoc_path, "build", "-i", ad_input, "-o", output, "-v", version]
        print("[Single Run]", " ".join(args))
    
        try:
            subprocess.run(args, check=True)
            messagebox.showinfo("Success", "Build completed successfully.")
        except subprocess.CalledProcessError as e:
            messagebox.showerror("Build Failed", f"Build failed:\n{e}")
            
            
    def _run_disassemble(self):
        config = parse_config(self.config_path)
        adhoc_path = config.get("ADHOC_DIR", "")
    
        adc_input = self.dis_input_var.get()
    
        if not os.path.isfile(adhoc_path):
            messagebox.showerror("Missing adhoc.exe", f"Adhoc executable not found:\n{adhoc_path}")
            return
    
        if not adc_input:
            messagebox.showwarning("Missing Input", "Please specify the .adc input file.")
            return
    
        args = [adhoc_path, adc_input]
    
        print("[Disassemble Run]", " ".join(args))
    
        try:
            subprocess.run(args, check=True)
            messagebox.showinfo("Success", "Disassembly completed successfully.")
        except subprocess.CalledProcessError as e:
            messagebox.showerror("Disassembly Failed", f"Disassembly failed:\n{e}")

if __name__ == "__main__":
    app = CommandLineWrapperApp()
    app.mainloop()
