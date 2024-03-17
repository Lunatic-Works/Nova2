class_name BaseBlock extends RefCounted

func pop_prefix(s: String, prefix: String, sep_len: int=0) -> Array:
    if s.begins_with(prefix):
        return [prefix, s.substr(prefix.length() + sep_len)]
    else:
        return [null, s]
