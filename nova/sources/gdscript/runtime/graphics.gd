class_name Graphics

#@export
static func move(obj: Variant, coord: Variant, scale=null, angle=null) -> void:
	obj = BuiltIn._get_obj(obj)
	if coord != null and not coord is Vector3:
		if scale == null:
			scale = BuiltIn._get_vec3(BuiltIn._get_index(coord, 2, null), obj.scale, func(s): return Vector3(s, s, 1))
		if angle == null:
			angle = BuiltIn._get_vec3(BuiltIn._get_index(coord, 4, null), obj.rotation_degrees, func(s): return Vector3(0, 0, s))

		coord = BuiltIn._get_vec3(coord, obj.position)
	if coord != null:
		obj.position = coord
	if scale != null:
		obj.scale = scale
	if angle != null:
		obj.rotation_degrees = angle

#@export
static func tint(obj, color) -> void:
	obj = BuiltIn._get_obj(obj)
	if not color is Color:
		var a = BuiltIn._get_index(color, 3, 1)
		color = Color(color[0], color[1], color[2], a)
	obj.modulate = color

#@export
static func show(obj, image_path, coord=null, color=null) -> void:
	obj = BuiltIn._get_obj(obj)

	var path = BuiltIn.c.resource_root
	if obj.has_meta("folder"):
		path += obj.get_meta("folder") + "/"
	path += image_path + ".png"

	if coord != null:
		move(obj, coord)
	if color != null:
		tint(obj, color)

	obj.texture = load(path)
	obj.visible = true

#@export
static func hide(obj) -> void:
	obj = BuiltIn._get_obj(obj)
	obj.visible = false
