class_name EagerBlock extends BaseBlock

var _script_loader: RefCounted

static var _last_display_name = null
static var _current_filename = null

func action_new_file(filename: String) -> void:
    _current_filename = filename
    _last_display_name = null

func _try_get_local_name(name):
    if name == null:
        return null

    var x = pop_prefix(name, "l_")
    if x[0] != null:
        return _current_filename + ":" + x[1]
    else:
        return name

func label(name: String, display_name=null) -> void:
    if display_name == null:
        if _last_display_name == null:
            display_name = name
        else:
            display_name = _last_display_name
    else:
        _last_display_name = display_name

    if _script_loader.IsDefaultLocale:
        _script_loader.RegisterNewNode(name, display_name)
    else:
        _script_loader.AddLocalizedNode(name, display_name)

func is_start() -> void:
    _script_loader.SetCurrentAsStart()

func is_unlocked_start() -> void:
    _script_loader.SetCurrentAsChapter()
    _script_loader.SetCurrentAsUnlockedStart()

func is_end(name=null) -> void:
    name = _try_get_local_name(name)
    _script_loader.SetCurrentAsEnd(name)

func __eval() -> void:
    push_error("Must override __eval in child")

func run(script_loader: RefCounted) -> void:
    _script_loader = script_loader
    __eval()
