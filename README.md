# nullinside-api-twitch-bot

[![Tests](https://github.com/nullinside-development-group/nullinside-api-twitch-bot/workflows/CodeQL/badge.svg)](https://github.com/nullinside-development-group/nullinside-api-twitch-bot/actions?query=workflow%3ACodeQL)

This is an API for configuring and running a twitch bot to run on Twitch.tv. This code is actively deployed to the
following twitch account at all times: [nullinside](https://www.twitch.tv/nullinside/about)

## Known Issues

1. **Error Message:** "You have an error in your SQL syntax; check the manual that corresponds to your MySQL server
   version for the right syntax to use near 'RETURNING..." on `.SaveChangesAsync()`
    * **Solution:** Cannot use the `.ValueGeneratedOnAdd()`, `.ValueGeneratedOnAddOrUpdate()`,
      or `.ValueGeneratedOnUpdate()` in the modeling POCOs.
    * **Description:** For whatever reason, these don't generate the correct SQL when you later perform an `UPDATE` on
      an unrelated field in the POCO and call `.SaveChangesAsync()`.