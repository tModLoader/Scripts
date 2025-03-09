let data = open servers.yaml
let active_general = $data.general | where {|x| not ("inactive" in $x)}
let active_mods = $data.mods | where {|x| not ("inactive" in $x)}

$'
# Helpful Servers
($active_general | each {|x| $"**($x.name)**\n- <($x.url)>"} | str join "\n")
# Mod-specific Servers
> To suggest a popular mod server for this list, please see <#1348388251693617173> under <#1118000566061244546>.

($active_mods | each {|x| $"**($x.name)**\n- <($x.url)>" } | str join "\n")
'