# Feed V3 Consumer Boilerplate C#

## Objective
Spread the best pratices and give a quick start to consume the VTEX Feed V3.

## Architceture
This same is using .net core 3.1 but this solution can be converted to another .net core version.

The approach leverages the background service netcore feature to keep processing data

In order to keep the request inside the throttling limits there is 2 semaphores:
- One controls the request in a period, usually  in a minute 
- Another controls the request concurrently


