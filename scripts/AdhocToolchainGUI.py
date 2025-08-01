try:
    import tkinter as tk
    from tkinter import ttk, filedialog, messagebox, simpledialog
    import os
    import subprocess
    import shutil
    import platform
    from pathlib import Path
    import threading
    from threading import Thread
except ImportError as e:
    import sys
    missing = str(e).split()[-1].strip("'")
    tk.Tk().withdraw()  # Hide root window
    messagebox.showerror(
        "Missing Dependency",
        f"The module '{missing}' is required but not installed.\n\n"
        "Please install it and try again."
    )
    sys.exit(1)

def launch_main_app(config_file):
    app = CommandLineWrapperApp(config_file)
    app.mainloop()
    
def is_path_in_env(path):
    norm_path = os.path.normpath(path)

    if platform.system() == "Windows":
        try:
            result = subprocess.run(
                ["powershell", "-Command", "[Environment]::GetEnvironmentVariable('PATH', 'User')"],
                capture_output=True, text=True, check=True
            )
            user_path = result.stdout.strip()
            return norm_path in map(os.path.normpath, user_path.split(os.pathsep))
        except subprocess.CalledProcessError:
            return False
    else:
        home = Path.home()
        shell = os.environ.get("SHELL", "")
        rc_file = None

        if "zsh" in shell:
            rc_file = home / ".zshrc"
        elif "bash" in shell:
            rc_file = home / ".bashrc"
        else:
            rc_file = home / ".profile"

        if rc_file.exists():
            try:
                with open(rc_file, "r", encoding="utf-8") as f:
                    return norm_path in f.read()
            except Exception:
                return False
        return False
        
        
def remove_adhoc_from_path(path, parent_window):
    def run():
        # Ensure we're comparing just the directory
        path_dir = os.path.dirname(path)

        progress = tk.Toplevel(parent_window)
        progress.title("Processing")
        progress.transient(parent_window)
        progress.grab_set()
        tk.Label(progress, text="Removing from PATH, please wait...").pack(padx=20, pady=20)
        center_window_top(progress, parent_window)
        parent_window.update()

        system = platform.system()
        removed = False

        if system == "Windows":
            try:
                current = subprocess.check_output(
                    ["powershell", "-Command", "[Environment]::GetEnvironmentVariable('PATH', 'User')"],
                    text=True
                ).strip()
                paths = current.split(";")
                if path_dir not in paths:
                    progress.destroy()
                    messagebox.showinfo("Not Found", "The Adhoc Toolchain folder was not found in PATH.")
                    return

                new_paths = [p for p in paths if p.strip() != path_dir]
                new_path_str = ";".join(new_paths)
                subprocess.run(
                    ["powershell", "-Command", f"[Environment]::SetEnvironmentVariable('PATH', '{new_path_str}', 'User')"],
                    check=True
                )
                removed = True
            except Exception as e:
                progress.destroy()
                messagebox.showerror("Error", f"Failed to remove from PATH:\n{e}")
                return

        else:  # Linux
            try:
                bashrc_path = os.path.expanduser("~/.bashrc")
                export_line = f'export PATH="{path_dir}:$PATH"'
                if not os.path.exists(bashrc_path):
                    progress.destroy()
                    messagebox.showinfo("Not Found", "The PATH entry was not found in .bashrc.")
                    return
                with open(bashrc_path, "r") as f:
                    lines = f.readlines()
                new_lines = [line for line in lines if export_line not in line]
                with open(bashrc_path, "w") as f:
                    f.writelines(new_lines)
                removed = True
            except Exception as e:
                progress.destroy()
                messagebox.showerror("Error", f"Failed to update .bashrc:\n{e}")
                return

        progress.destroy()
        if removed:
            messagebox.showinfo("Success", "Adhoc Toolchain folder removed from PATH successfully.")

    Thread(target=run).start()
    
    
def add_to_path_unix(path):
    shell = os.environ.get("SHELL", "")
    home = Path.home()
    rc_file = None

    if "zsh" in shell:
        rc_file = home / ".zshrc"
    elif "bash" in shell:
        rc_file = home / ".bashrc"
    else:
        rc_file = home / ".profile"

    export_line = f'\n# Added by Adhoc GUI\nexport PATH="$PATH:{path}"\n'

    try:
        with open(rc_file, "a") as f:
            f.write(export_line)
        return True
    except Exception:
        return False
        
    
def add_to_path_windows(path):
    cmd = [
        "powershell",
        "-Command",
        f"[Environment]::SetEnvironmentVariable('PATH', [Environment]::GetEnvironmentVariable('PATH', 'User') + ';{path}', 'User')"
    ]
    try:
        subprocess.run(cmd, check=True)
        return True
    except subprocess.CalledProcessError:
        return False

def select_config_profile():
    profile_window = tk.Tk()
    profile_window.title("Select Profile")
    profile_window.geometry("400x150")
    profile_window.eval('tk::PlaceWindow . center')
    profile_window.resizable(False, False)

    ttk.Label(profile_window, text="Select or create a profile:", font=("Arial", 12)).pack(pady=10)

    config_files = [f for f in os.listdir() if f.startswith("adhocguiconfig_") and f.endswith(".txt")]
    if not config_files:
        config_files.append("")  # force dropdown to exist, placeholder until user creates one
    profile_map = {f.replace("adhocguiconfig_", "").replace(".txt", ""): f for f in config_files}
    display_names = list(profile_map.keys())
    selected_profile = tk.StringVar(value=display_names[0])
    dropdown = ttk.Combobox(profile_window, textvariable=selected_profile, values=display_names, state="readonly", width=35)
    dropdown.pack(pady=5)

    def use_selected():
        selected = profile_map[selected_profile.get()]
        if selected and os.path.isfile(selected):
            profile_window.destroy()
            launch_main_app(selected)
        else:
            messagebox.showerror("No Profile Selected", "Please select a valid profile.")

    def create_new():
        def save_new():
            name = name_var.get().strip()
            if not name:
                messagebox.showerror("Invalid Name", "Please enter a valid profile name.")
                return
            filename = f"adhocguiconfig_{name}.txt"
            if os.path.exists(filename):
                messagebox.showerror("Already Exists", "A profile with this name already exists.")
                return
            with open(filename, "w", encoding="utf-8") as f:
                f.write("// New profile\n")
            profile_window.destroy()
            launch_main_app(filename)

        new_win = tk.Toplevel(profile_window)
        new_win.title("Create New Profile")
        new_win.geometry("300x120")
        center_window_top(new_win)

        ttk.Label(new_win, text="Enter profile name:").pack(pady=10)
        name_var = tk.StringVar()
        ttk.Entry(new_win, textvariable=name_var).pack(pady=5)
        ttk.Button(new_win, text="Create", command=save_new).pack(pady=10)

    bottom_frame = ttk.Frame(profile_window)
    bottom_frame.pack(pady=10)

    ttk.Button(bottom_frame, text="Start", command=use_selected).pack(side="left", padx=5)
    ttk.Button(bottom_frame, text="Create New Profile", command=create_new).pack(side="right", padx=5)

    profile_window.mainloop()
    
def _create_new_config_profile():
    import tkinter.simpledialog as simpledialog
    while True:
        profile_name = simpledialog.askstring("New Profile", "Enter a name for your new profile:")
        if profile_name is None:
            return None  # Cancelled
        profile_name = profile_name.strip()
        if profile_name == "":
            continue
        filename = f"adhocguiconfig_{profile_name}.txt"
        if os.path.exists(filename):
            messagebox.showwarning("File Exists", "That profile already exists. Choose another name.")
            continue
        return _validate_and_initialize_config(filename)
        
def _validate_and_initialize_config(filename):
    config_path = os.path.abspath(filename)
    config_data = {}

    # Read existing if present
    if os.path.exists(config_path):
        with open(config_path, "r", encoding="utf-8") as f:
            for line in f:
                if "=" in line:
                    key, val = line.strip().split("=", 1)
                    config_data[key.strip()] = val.strip().strip('"')

    # Prompt for adhoc.exe if not defined
    if "ADHOC_DIR" not in config_data:
        messagebox.showinfo(
            "First-Time Setup",
            "Please locate the Adhoc Toolchain executable for this profile."
        )
        initial_dir = os.path.dirname(os.path.abspath(__file__))
        adhoc_path = filedialog.askopenfilename(
            title="Select Adhoc Toolchain Executable",
            filetypes=[("Executable", "*.exe")],
            initialdir=initial_dir
        )
        if not adhoc_path:
            messagebox.showerror("Error", "No executable selected. Exiting.")
            return None

        config_data["ADHOC_DIR"] = adhoc_path
        config_data.setdefault("DEFAULT_TAB", "yaml")
        config_data.setdefault("INPUT_DIR", "")
        config_data.setdefault("OUTPUT_DIR", "")

        try:
            with open(config_path, "w", encoding="utf-8") as f:
                for key, val in config_data.items():
                    f.write(f'{key} = "{val}"\n')
        except Exception as e:
            messagebox.showerror("Error", f"Failed to write config file:\n{e}")
            return None

    return config_path

def _prompt_user_to_select_config(config_files):
    selected_file = {"value": None}

    def confirm_selection():
        selected_file["value"] = dropdown.get()
        root.destroy()

    root = tk.Tk()
    root.title("Select Adhoc Config Profile")
    root.geometry("400x120")
    root.resizable(False, False)
    root.eval('tk::PlaceWindow . center')

    ttk.Label(root, text="Select a configuration profile to load:", font=("Arial", 11)).pack(pady=10)

    dropdown = ttk.Combobox(root, values=config_files, state="readonly", width=40)
    dropdown.current(0)
    dropdown.pack()

    ttk.Button(root, text="OK", command=confirm_selection).pack(pady=10)

    root.mainloop()
    return selected_file["value"]
    
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
    
def center_window_top(win, parent=None):
    win.update_idletasks()

    if parent:
        parent_x = parent.winfo_rootx()
        parent_y = parent.winfo_rooty()
        parent_width = parent.winfo_width()
        parent_height = parent.winfo_height()

        window_width = win.winfo_width()
        window_height = win.winfo_height()

        x = parent_x + (parent_width // 2) - (window_width // 2)
        y = parent_y + (parent_height // 2) - (window_height // 2)
    else:
        screen_width = win.winfo_screenwidth()
        screen_height = win.winfo_screenheight()

        window_width = win.winfo_width()
        window_height = win.winfo_height()

        x = (screen_width // 2) - (window_width // 2)
        y = (screen_height // 2) - (window_height // 2)

    win.geometry(f"+{x}+{y}")
    
def add_adhoc_to_path(path, parent_window):
    def run():
        # Ensure path is a folder, not the executable
        path_dir = os.path.dirname(path)

        progress = tk.Toplevel(parent_window)
        progress.title("Processing")
        progress.transient(parent_window)
        progress.grab_set()
        tk.Label(progress, text="Adding to PATH, please wait...").pack(padx=20, pady=20)
        center_window_top(progress, parent_window)
        parent_window.update()

        system = platform.system()
        added = False

        if system == "Windows":
            try:
                current = subprocess.check_output(
                    ["powershell", "-Command", "[Environment]::GetEnvironmentVariable('PATH', 'User')"],
                    text=True
                ).strip()
                if path_dir in current.split(";"):
                    progress.destroy()
                    messagebox.showinfo("Already Exists", "The Adhoc Toolchain folder is already in PATH.")
                    return

                new_path = current + (";" if current else "") + path_dir
                subprocess.run(
                    ["powershell", "-Command", f"[Environment]::SetEnvironmentVariable('PATH', '{new_path}', 'User')"],
                    check=True
                )
                added = True
            except Exception as e:
                progress.destroy()
                messagebox.showerror("Error", f"Failed to add to PATH:\n{e}")
                return

        else:  # Linux
            try:
                bashrc_path = os.path.expanduser("~/.bashrc")
                export_line = f'export PATH="{path_dir}:$PATH"'
                already_present = False
                if os.path.exists(bashrc_path):
                    with open(bashrc_path, "r") as f:
                        already_present = any(export_line in line for line in f)
                if already_present:
                    progress.destroy()
                    messagebox.showinfo("Already Exists", "The Adhoc Toolchain folder is already in PATH.")
                    return
                with open(bashrc_path, "a") as f:
                    f.write(f"\n{export_line}\n")
                added = True
            except Exception as e:
                progress.destroy()
                messagebox.showerror("Error", f"Failed to update .bashrc:\n{e}")
                return

        progress.destroy()
        if added:
            messagebox.showinfo("Success", "Adhoc Toolchain folder added to PATH successfully.")

    Thread(target=run).start()
    
def show_progress(message, parent):
    popup = Toplevel(parent)
    popup.title("Please wait...")
    popup.geometry("300x100")
    popup.grab_set()
    popup.transient(parent)
    Label(popup, text=message).pack(expand=True)
    popup.update()
    return popup
    

class QuickBuildTab(ttk.Frame):
    def __init__(self, parent, config, config_path):
        super().__init__(parent)
        self.quick_build_entries = []
        self.auto_diss_var = tk.BooleanVar(value=config.get("AUTO_DISS_ON_QUICKBUILD", False))
        self.config_path = config_path
        self.config_data = config
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

        auto_diss_check = ttk.Checkbutton(top_frame, text="Auto Disassemble on Build", variable=self.auto_diss_var, command=self.save_to_config)
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
    
            run_btn = ttk.Button(row, text="Build", width=16, command=lambda i=index: self._run_entry(i))
            run_btn.pack(side="left", padx=20)
            
            openInput_btn = ttk.Button(row, text="Go to Source", command=lambda i=index: self._openInput(i))
            openInput_btn.pack(side="left", padx=2)
            
            openOutput_btn = ttk.Button(row, text="Go to Output", command=lambda i=index: self._openOutput(i))
            openOutput_btn.pack(side="left", padx=2)
    
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
    
        def center_window(win, parent):
            win.update_idletasks()
            parent_x = parent.winfo_rootx()
            parent_y = parent.winfo_rooty()
            parent_width = parent.winfo_width()
            parent_height = parent.winfo_height()
        
            window_width = win.winfo_width()
            window_height = win.winfo_height()
        
            x = parent_x + (parent_width // 2) - (window_width // 2)
            y = parent_y + (parent_height // 2) - (window_height // 2)
        
            win.geometry(f"+{x}+{y}")
    
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
    
        # dunno if i still need this
        def browse_file(var):
            path = filedialog.askopenfilename(title="Select File")
            if path:
                var.set(path)
                
                
        def browse_input():
            initial_dir = self.config_data.get("INPUT_DIR", "")
            if not os.path.isdir(initial_dir):
                initial_dir = os.path.dirname(ad_input_var.get() or yaml_input_var.get())
            filetypes = [("All files", "*.*")]
            if mode_var.get() == "YAML":
                filetypes = [("YAML Files", "*.yaml")]
            else:
                filetypes = [("AD Files", "*.ad")]
            path = filedialog.askopenfilename(title="Select Input File", initialdir=initial_dir, filetypes=filetypes)
            if path:
                if mode_var.get() == "YAML":
                    yaml_input_var.set(path)
                else:
                    ad_input_var.set(path)
                    
                    
        def browse_output():
            initial_dir = self.config_data.get("OUTPUT_DIR", "")
            if not os.path.isdir(initial_dir):
                initial_dir = os.path.dirname(output_adc_var.get())
            path = filedialog.asksaveasfilename(title="Select Output .adc File", initialdir=initial_dir,
                                                defaultextension=".adc", filetypes=[("ADC Files", "*.adc")])
            if path:
                output_adc_var.set(path)
    
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
        ad_browse_btn = ttk.Button(ad_row, text="Browse", command=lambda: browse_input())
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
        yaml_browse_btn = ttk.Button(yaml_row, text="Browse", command=lambda: browse_input())
        yaml_browse_btn.pack(side="left", padx=5)
    
        # --- Output .adc ---
        ttk.Label(win, text="Output .adc:").pack(anchor="w", padx=10)
        out_row = ttk.Frame(win)
        out_row.pack(fill="x", padx=10, pady=2)
        output_adc_entry = ttk.Entry(out_row, textvariable=output_adc_var)
        output_adc_entry.pack(side="left", fill="x", expand=True)
        out_browse_btn = ttk.Button(out_row, text="Browse", command=lambda: browse_output())
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
        center_window(win, self.winfo_toplevel())

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
        self.config_data["AUTO_DISS_ON_QUICKBUILD"] = str(self.auto_diss_var.get()).lower()
        lines = [line for line in lines if not line.strip().startswith("AUTO_DISS_ON_QUICKBUILD")]
        lines.append(f"AUTO_DISS_ON_QUICKBUILD = {self.config_data['AUTO_DISS_ON_QUICKBUILD']}\n")
    
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                f.writelines(lines)
            print("[Config] Saved successfully.")
        except Exception as e:
            print(f"[Config] Failed to save: {e}")
            
            
    def _openInput(self, index):
        entry = self.quick_build_entries[index]
        path = entry["yaml_input"] if entry["mode"] == "YAML" else entry["ad_input"]
        folder = os.path.dirname(path)
        if folder and os.path.exists(folder):
            self._open_folder(folder)
        else:
            messagebox.showwarning("Invalid Path", "Input file path is empty or does not exist.")
        
    def _openOutput(self, index):
        entry = self.quick_build_entries[index]
        folder = os.path.dirname(entry["output_adc"])
        if folder and os.path.exists(folder):
            self._open_folder(folder)
        else:
            messagebox.showwarning("Invalid Path", "Output file path is empty or does not exist.")
            
    def _open_folder(self, path):
        try:
            if platform.system() == "Windows":
                os.startfile(path)
            elif platform.system() == "Darwin":
                subprocess.Popen(["open", path])
            else:
                subprocess.Popen(["xdg-open", path])
        except Exception as e:
            messagebox.showerror("Error", f"Could not open folder:\n{e}")
            
class CommandLineWrapperApp(tk.Tk):
    def __init__(self, config_path):
        super().__init__()
        profile_name = config_path.replace("adhocguiconfig_", "").replace(".txt", "")
        self.title(f"Adhoc Toolchain GUI Wrapper - {profile_name}")
        if platform.system() == "Windows":
            self.geometry("800x600")
        else:
            self.geometry("880x600") # Linux needs a lil more width otherwise delete button gets cut off
        # Load config
        self.config_path = config_path
        self.config_data = parse_config(self.config_path)
    
        # First-time setup: No config or missing ADHOC_DIR
        if not self.config_data or "ADHOC_DIR" not in self.config_data:
            messagebox.showinfo(
                "First-Time Setup",
                "This appears to be your first time using this profile.\nPlease locate the Adhoc Toolchain executable."
            )
            if platform.system() == "Windows":
                filetypes = [("Adhoc executable", "*.exe")]
            else:
                filetypes = [("Adhoc executable", "*")]  # Linux
            adhoc_path = filedialog.askopenfilename(
                title="Select Adhoc Toolchain Executable",
                filetypes=filetypes,
                initialdir=os.path.dirname(os.path.abspath(__file__)),
            )
    
            if not adhoc_path:
                messagebox.showerror("Error", "Adhoc executable was not selected. Exiting.")
                self.destroy()
                return
    
            self.config_data["ADHOC_DIR"] = adhoc_path
            self._write_initial_config()
    
        self.create_tabs()
    
        # Select default tab
        tab_key = self.config_data.get("DEFAULT_TAB", "yaml").lower()
        tab_name = TAB_KEYS.get(tab_key, "YAML")
        if tab_name in TAB_KEYS.values():
            idx = list(TAB_KEYS.values()).index(tab_name)
            self.tab_control.select(idx)
            
    def _write_initial_config(self):
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                f.write(f'ADHOC_DIR = "{self.config_data["ADHOC_DIR"]}"\n')
                f.write('DEFAULT_TAB = yaml\n')
            print(f"[Init] Created new config: {self.config_path}")
        except Exception as e:
            messagebox.showerror("Error", f"Failed to write initial config:\n{e}")
            self.destroy()
        
    def _first_time_setup(self):
        messagebox.showinfo("First-time Setup", "Adhoc Toolchain location not specified. Press OK to locate adhoc.exe on your system")
        initial_dir = os.path.dirname(os.path.abspath(__file__))
        exe_path = filedialog.askopenfilename(
            title="Locate Adhoc Toolchain execuatble",
            filetypes=[("Executable", "*.exe")],
            initialdir=initial_dir
        )
    
        if not exe_path:
            messagebox.showerror("Setup Incomplete", "Adhoc Toolchain execuatble path was not selected. Exiting.")
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
        
        # INPUT_DIR
        ttk.Label(frame, text="Set auto-jump to source projects on quick build configure:").grid(row=2, column=0, sticky="w")
        self.input_dir_var = tk.StringVar(value=self.config_data.get("INPUT_DIR", ""))
        ttk.Entry(frame, textvariable=self.input_dir_var, width=60).grid(row=3, column=0)
        ttk.Button(frame, text="Browse", command=self._browse_input_dir).grid(row=3, column=2)
    
        # OUTPUT_DIR
        ttk.Label(frame, text="Set auto-jump to output projects on quick build configure:").grid(row=4, column=0, sticky="w")
        self.output_dir_var = tk.StringVar(value=self.config_data.get("OUTPUT_DIR", ""))
        ttk.Entry(frame, textvariable=self.output_dir_var, width=60).grid(row=5, column=0)
        ttk.Button(frame, text="Browse", command=self._browse_output_dir).grid(row=5, column=2)
        
        # 2. Default tab selection
        ttk.Label(frame, text="Default Tab on Startup:").grid(row=6, column=0, sticky="w", pady=(10, 0))
        tab_names = list(TAB_KEYS.values())
        self.default_tab_var = tk.StringVar(value=TAB_KEYS.get(self.config_data.get("DEFAULT_TAB", "yaml"), "YAML"))
        ttk.Combobox(frame, textvariable=self.default_tab_var, values=tab_names, state="readonly").grid(row=7, column=0, sticky="w")
    
        # 3. Credits box
        credits_text = (
            "Adhoc Toolchain GUI Wrapper by Silentwarior112\n"
            "Built for modding workflows\n"
        )
        ttk.Label(frame, text="Credits:").grid(row=8, column=0, sticky="w", pady=(20, 0))
        credits_box = tk.Text(frame, height=5, width=50, wrap="word")
        credits_box.grid(row=9, column=0, sticky="w")
        credits_box.insert("1.0", credits_text)
        credits_box.configure(state="disabled") 
    
        # Save settings button
        ttk.Button(frame, text="Save Settings", command=self._save_settings).grid(row=10, column=0, pady=20, sticky="w")
        
        # Profile management buttons
        ttk.Button(frame, text="Load Profile", command=self._load_profile_from_settings).grid(row=10, column=1, pady=20, sticky="w")
        ttk.Button(frame, text="Create New Profile", command=self._create_new_profile_from_settings).grid(row=10, column=2, pady=20, sticky="w")
        
        # Add Adhoc Toolchain to environment variables
        ttk.Label(frame, text="Add Adhoc Toolchain to environment variables").grid(row=15, column=0, sticky="w")
        ttk.Button(frame, text="Add to PATH", command=lambda: add_adhoc_to_path(self.adhoc_path_var.get(), self)).grid(row=16, column=0, sticky="w", pady=(2, 10))
        ttk.Button(frame, text="Remove from PATH", command=lambda: remove_adhoc_from_path(self.adhoc_path_var.get(), self)).grid(row=16, column=1, sticky="w", pady=(2, 10))
        
    def _browse_adhoc_path(self):
        path = filedialog.askopenfilename(title="Locate adhoc.exe", filetypes=[("Executable", "*.exe")])
        if path:
            self.adhoc_path_var.set(path)
            
    def _browse_input_dir(self):
        folder = filedialog.askdirectory()
        if folder:
            self.input_dir_var.set(folder)
    
    def _browse_output_dir(self):
        folder = filedialog.askdirectory()
        if folder:
            self.output_dir_var.set(folder)
            
            
    def _create_new_profile_from_settings(self):
        popup = tk.Toplevel(self)
        popup.title("Create New Profile")
        popup.geometry("400x160")  # Set desired size
        center_window_top(popup, self)  # Center relative to settings window
        popup.grab_set()
    
        tk.Label(popup, text="Enter new profile name:").pack(pady=(15, 5))
        entry = ttk.Entry(popup, width=30)
        entry.pack()
    
        def save():
            name = entry.get().strip()
            if name:
                filename = f"adhocguiconfig_{name}.txt"
                path = os.path.join(os.getcwd(), filename)
                if not os.path.exists(path):
                    with open(path, "w", encoding="utf-8") as f:
                        f.write("")  # Create an empty profile file
                    messagebox.showinfo("Profile Created", f"Profile '{name}' created. Select Load Profile to load the new profile.")
                    popup.destroy()
                else:
                    messagebox.showwarning("Profile Exists", "Profile already exists.")
    
        ttk.Button(popup, text="Create", command=save).pack(pady=10)
        
    def _load_profile_from_settings(self):
        all_files = [f for f in os.listdir() if f.startswith("adhocguiconfig_") and f.endswith(".txt")]
        if not all_files:
            messagebox.showinfo("No Profiles", "No profiles found to load.")
            return
    
        profile_map = {f.replace("adhocguiconfig_", "").replace(".txt", ""): f for f in all_files}
        display_names = list(profile_map.keys())
    
        popup = tk.Toplevel(self)
        popup.title("Load Profile")
        popup.resizable(False, False)
        center_window_top(popup, self)
    
        ttk.Label(popup, text="Select a profile to load:").pack(pady=10)
    
        selected_name = tk.StringVar(value=display_names[0])
        dropdown = ttk.Combobox(popup, textvariable=selected_name, values=display_names, state="readonly", width=50)
        dropdown.pack(pady=5)
    
        def on_confirm():
            popup.destroy()
            self.destroy()
            selected_file = profile_map[selected_name.get()]
            launch_main_app(selected_file)
    
        ttk.Button(popup, text="Load", command=on_confirm).pack(pady=5)
    
    def _save_settings(self):
        # Update config data
        self.config_data["ADHOC_DIR"] = self.adhoc_path_var.get()
        self.config_data["INPUT_DIR"] = self.input_dir_var.get()
        self.config_data["OUTPUT_DIR"] = self.output_dir_var.get()
    
        tab_key = [k for k, v in TAB_KEYS.items() if v == self.default_tab_var.get()]
        self.config_data["DEFAULT_TAB"] = tab_key[0] if tab_key else "yaml"
    
        # Rewrite config file
        try:
            with open(self.config_path, "r", encoding="utf-8") as f:
                lines = f.readlines()
        except FileNotFoundError:
            lines = []
    
        # Remove affected lines
        lines = [line for line in lines if not line.strip().startswith(("ADHOC_DIR", "DEFAULT_TAB", "INPUT_DIR", "OUTPUT_DIR"))]
    
        # Add updated lines
        lines.append(f'ADHOC_DIR = "{self.config_data["ADHOC_DIR"]}"\n')
        lines.append(f'DEFAULT_TAB = {self.config_data["DEFAULT_TAB"]}\n')
        lines.append(f'INPUT_DIR = "{self.config_data["INPUT_DIR"]}"\n')
        lines.append(f'OUTPUT_DIR = "{self.config_data["OUTPUT_DIR"]}"\n')
    
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
    selected_config_file = select_config_profile()
    if selected_config_file is None:
        exit()

    CONFIG_FILE = selected_config_file
    app = CommandLineWrapperApp()
    app.mainloop()
