import copy

NOF_PLAYERS = 3
STARTING_PLAYER = 2 # 0 for 2 players
STACK = 50
BB = 2
SB = 1
RAISES = [0.5, 1, 5] # part of pot?

debug = False
player_stacks_g = [STACK]*NOF_PLAYERS
last_actions_g = ['R', 'R']+['0']*(NOF_PLAYERS-2)
player_bets_g = [0]*NOF_PLAYERS
player_stacks_g[0] -= SB
player_bets_g[0] += SB
player_stacks_g[1] -= BB
player_bets_g[1] += BB

# counts states of the game in action space (not just from one player's view)
# only first round
def recursive_count(curr_player, last_raiser, last_raise, curr_call, player_stacks, last_actions):
    res = 0

    # all but one player in round, round over
    if sum(1 for action in last_actions if action == 'F') == NOF_PLAYERS -1:
        return res

    # raise
    if last_raiser != curr_player:
        for raise_fraction in RAISES:
            RAISE = raise_fraction* (STACK*NOF_PLAYERS - sum([bet for bet in player_stacks]))
            if RAISE > last_raise and RAISE + (curr_call - (STACK - player_stacks[curr_player])) < player_stacks[curr_player]:
                player_stacks_temp = copy.copy(player_stacks)
                player_stacks_temp[curr_player] -= RAISE + (curr_call - (STACK - player_stacks[curr_player]))
                last_actions_temp = copy.copy(last_actions)
                last_actions_temp[curr_player] = 'R'

                if debug:
                    print(last_actions_temp)
                    print(player_stacks_temp)

                res += 1 + recursive_count((curr_player+1)%NOF_PLAYERS, curr_player, RAISE,
                                           STACK - player_stacks_temp[curr_player], player_stacks_temp, last_actions_temp)

    # all in
    if player_stacks[curr_player] > 0: # enough chips for all in
        if last_actions[curr_player] != 'F': # didnt fold last round
            same = True
            temp_chips = -1
            for i in range(NOF_PLAYERS):
                if temp_chips == -1:
                    temp_chips = player_stacks[i]
                if last_actions[i] != 'F' and player_stacks[i] != temp_chips:
                    same = False
                    break
            if not same:
                player_stacks_temp = copy.copy(player_stacks)
                player_stacks_temp[curr_player] = 0

                last_actions_temp = copy.copy(last_actions)
                last_actions_temp[curr_player] = 'A'

                if debug:
                    print(last_actions_temp)
                    print(player_stacks_temp)

                if STACK - curr_call >= last_raise: # reopen betting if high enough all in
                    res += 1 + recursive_count((curr_player+1)%NOF_PLAYERS, curr_player, STACK,
                                               STACK, player_stacks_temp, last_actions_temp)
                else:
                    res += 1 + recursive_count((curr_player+1)%NOF_PLAYERS, last_raiser, last_raise,
                                               STACK, player_stacks_temp, last_actions_temp)

    # calls
    if curr_call - (STACK - player_stacks[curr_player]) < player_stacks[curr_player] and \
            curr_call - (STACK - player_stacks[curr_player]) > 0:
        player_stacks_temp = copy.copy(player_stacks)
        player_stacks_temp[curr_player] -=  curr_call - (STACK - player_stacks[curr_player])
        last_actions_temp = copy.copy(last_actions)
        last_actions_temp[curr_player] = 'C'

        if debug:
            print(last_actions_temp)
            print(player_stacks_temp)

        res += 1 + recursive_count((curr_player+1)%NOF_PLAYERS, last_raiser, last_raise,
                                   curr_call, player_stacks_temp, last_actions_temp)

    # check
    if curr_call == (STACK - player_stacks[curr_player]) and last_actions[curr_player] != 'A' and \
        last_actions[curr_player] != 'C':
        player_stacks_temp = copy.copy(player_stacks)
        last_actions_temp = copy.copy(last_actions)
        last_actions_temp[curr_player] = 'CH'

        if debug:
            print(last_actions_temp)
            print(player_stacks_temp)

        res += 1 + recursive_count((curr_player + 1) % NOF_PLAYERS, last_raiser, last_raise,
                                   curr_call, player_stacks_temp, last_actions_temp)

    # fold
    if curr_call > STACK - player_stacks[curr_player] and last_actions[curr_player] != 'F':
        player_stacks_temp = copy.copy(player_stacks)
        last_actions_temp = copy.copy(last_actions)
        last_actions_temp[curr_player] = 'F'

        if debug:
            print(last_actions_temp)
            print(player_stacks_temp)

        res += 1 + recursive_count((curr_player + 1) % NOF_PLAYERS, last_raiser, last_raise,
                                   curr_call, player_stacks_temp, last_actions_temp)

    if debug:
        print("BACKUP")
    return res

print("{} states".format(recursive_count(STARTING_PLAYER, 1, 1, 2, player_stacks_g, last_actions_g)))
