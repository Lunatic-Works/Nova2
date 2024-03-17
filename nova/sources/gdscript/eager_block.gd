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

## define a node
func label(name: String, display_name=null) -> void:
    if display_name == null:
        if _last_display_name == null:
            display_name = name
        else:
            display_name = _last_display_name
    else:
        _last_display_name = display_name

    name = _try_get_local_name(name)

    if _script_loader.IsDefaultLocale:
        _script_loader.RegisterNewNode(name, display_name)
    else:
        _script_loader.AddLocalizedNode(name, display_name)

## jump to the given destination
## should be called at the end of the node
func jump_to(dest: String) -> void:
    dest = _try_get_local_name(dest)
    _script_loader.RegisterJump(dest)

## add branches to the current node
## should be called at the end of the node
## should be called only once for each node, i.e., all branches of the node should be added at once
## branches should be an array of 'branch'. A 'branch' is a dict with the following structure:
## {
##    dest: "name of the destination node"
##    text: "text on the button", should not use if mode is jump
##    image: {name: "image_name", x: x, y: y, scale: scale}, should not use if mode is jump
##    mode: "normal|jump|show|enable", optional, default is normal
##    cond: an expression string that returns a bool, should not use if mode is show
## }
func branch(branches: Array) -> void:
    for i in branches.size():
        var entry = branches[i]

        var name = str(i)
        var dest = _try_get_local_name(entry["dest"])
        var image_info = entry.get("image", null)
        var mode = entry.get("mode", "normal")
        var cond = entry.get("cond", "")

        if _script_loader.IsDefaultLocale:
            _script_loader.RegisterBranch(name, dest, entry["text"], image_info, mode, cond)
        else:
            _script_loader.AddLocalizedBranch(name, dest, entry["text"])

    _script_loader.EndRegisterBranch()

## set the current node as a chapter
func is_chapter() -> void:
    _script_loader.SetCurrentAsChapter()

## set the current node as a start node
## a game can have multiple start points
## which means this function can be called several times under different nodes
func is_start() -> void:
    _script_loader.SetCurrentAsStart()

## set the current node as a start point which is unlocked initially
## indicates is_chapter() and is_start()
func is_unlocked_start() -> void:
    _script_loader.SetCurrentAsChapter()
    _script_loader.SetCurrentAsUnlockedStart()

## set the current node as a debug node
## debug nodes can be entered by holding debug key in start game
func is_debug() -> void:
    _script_loader.SetCurrentAsDebug()

## set the current node as an end node
## should be called at the end of the node
## a name can be assigned to an end point, which can differ from the node name
## the name should be unique among all end point names
## if no name is given, the name of the current node will be used
## all nodes without child nodes should be marked as end nodes
## if is_end() is not called under those nodes, they will be marked as end nodes automatically
func is_end(name=null) -> void:
    name = _try_get_local_name(name)
    _script_loader.SetCurrentAsEnd(name)

func __eval() -> void:
    push_error("Must override __eval in child")

func run(script_loader: RefCounted) -> void:
    _script_loader = script_loader
    __eval()
