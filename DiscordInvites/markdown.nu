let data = open servers.yaml

def serverList [servers: list, --sort] {
    $servers
        | where {|x| not ("inactive" in $x)}
        | (if $sort { sort-by -i "title" } else {})
        | each {|x| $"**($x.title)**\n- <($x.discord   )>"}
        | str join "\n"
}

$'
# Helpful Servers
(serverList $data.general)
# Mod-specific Servers
> To suggest a popular mod server for this list, please see <#1348388251693617173> under <#1118000566061244546>.

(serverList $data.mods --sort)
'
