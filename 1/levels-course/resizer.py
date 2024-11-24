for d in (2, 7, 12, 17, 22):
    for i in range(1, 11):
        with open(f"level{d}-{i}.txt") as f:
            s = f.read().strip()
        s_s = s.strip().split('\n')
        for j in (-3, -4):
            s_s[j] = s_s[j].replace("0", "").replace("1", "").replace("9", "")
            if len(s_s[j]) != 0:
                print(d, i)
                break
        s_s = list(filter(lambda x: x != '', s_s))
        for k, line in enumerate(s_s):
            if line[-3] not in '910' or line[-4] not in '910':
                print(d, i)
                break
            s_s[k] = s_s[k][:-4] + s_s[k][-2:]

        with open(f"level{d}-{i}.txt", "w") as f:
            f.write('\n'.join(s_s))