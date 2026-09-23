# Question rankings

Top five shown below. All ranks and source text, raw latency samples, evidence spans and plans are preserved in retrieval.json. Offsets are UTF-16, zero-based, end exclusive.

## paragraph / pg-sql

How can I load a previously exported SQL database into the rehearsal database?

Answerable: True. Expected: postgres-backup / Restoring a plain SQL backup. First relevant rank: 1; similarity: 0.5357; highest irrelevant similarity: 0.5204.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 3 | Restoring a plain SQL backup | 1399..1835 | 87 | 0.4643 | 0.5357 | True |
| 2 | postgres-backup / 2 | Creating a backup | 969..1397 | 78 | 0.4796 | 0.5204 | False |
| 3 | postgres-backup / 4 | Restoring a plain SQL backup | 1837..2301 | 80 | 0.5504 | 0.4496 | False |
| 4 | postgres-backup / 5 | Restoring a custom archive | 2303..2765 | 88 | 0.5955 | 0.4045 | False |
| 5 | postgres-backup / 1 | Creating a backup | 536..967 | 86 | 0.6036 | 0.3964 | False |

## paragraph / pg-retention

How many daily and monthly database exports does the workshop retain?

Answerable: True. Expected: postgres-backup / Retention and offsite copies. First relevant rank: 1; similarity: 0.5997; highest irrelevant similarity: 0.5011.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 6 | Retention and offsite copies | 2767..3239 | 83 | 0.4003 | 0.5997 | True |
| 2 | postgres-backup / 4 | Restoring a plain SQL backup | 1837..2301 | 80 | 0.4989 | 0.5011 | False |
| 3 | postgres-backup / 7 | Point-in-time recovery boundary | 3241..3710 | 87 | 0.5411 | 0.4589 | False |
| 4 | postgres-backup / 2 | Creating a backup | 969..1397 | 78 | 0.5539 | 0.4461 | False |
| 5 | postgres-backup / 0 | PostgreSQL backup and restore | 0..534 | 99 | 0.6101 | 0.3899 | False |

## paragraph / pg-custom

Which restore program should I use for a custom-format database archive?

Answerable: True. Expected: postgres-backup / Restoring a custom archive. First relevant rank: 2; similarity: 0.5219; highest irrelevant similarity: 0.5784.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 2 | Creating a backup | 969..1397 | 78 | 0.4216 | 0.5784 | False |
| 2 | postgres-backup / 5 | Restoring a custom archive | 2303..2765 | 88 | 0.4781 | 0.5219 | True |
| 3 | postgres-backup / 0 | PostgreSQL backup and restore | 0..534 | 99 | 0.5461 | 0.4539 | False |
| 4 | postgres-backup / 9 | Point-in-time recovery boundary | 3780..4004 | 42 | 0.6361 | 0.3639 | False |
| 5 | postgres-backup / 1 | Creating a backup | 536..967 | 86 | 0.6426 | 0.3574 | False |

## paragraph / deploy-probes

Which probe removes a Pod from normal traffic, and which one triggers a restart?

Answerable: True. Expected: deployments / Readiness and liveness checks. First relevant rank: 1; similarity: 0.5526; highest irrelevant similarity: 0.5416.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | deployments / 3 | Readiness and liveness checks | 1456..1953 | 89 | 0.4474 | 0.5526 | True |
| 2 | services / 2 | Selecting application Pods | 965..1412 | 76 | 0.4584 | 0.5416 | False |
| 3 | services / 9 | Investigation checklist | 3750..4039 | 53 | 0.5038 | 0.4962 | False |
| 4 | deployments / 2 | Rolling out a new application image | 986..1454 | 88 | 0.5151 | 0.4849 | False |
| 5 | deployments / 7 | Scaling and routing boundaries | 3276..3778 | 90 | 0.5316 | 0.4684 | False |

## paragraph / deploy-rollback

How do I revert a failed application release, and will that undo database migrations?

Answerable: True. Expected: deployments / Rollback procedure. First relevant rank: 2; similarity: 0.4793; highest irrelevant similarity: 0.4993.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | deployments / 6 | Rollback procedure | 2901..3274 | 66 | 0.5007 | 0.4993 | False |
| 2 | deployments / 5 | Rollback procedure | 2418..2899 | 85 | 0.5207 | 0.4793 | True |
| 3 | postgres-backup / 4 | Restoring a plain SQL backup | 1837..2301 | 80 | 0.6347 | 0.3653 | False |
| 4 | postgres-backup / 5 | Restoring a custom archive | 2303..2765 | 88 | 0.6672 | 0.3328 | False |
| 5 | postgres-backup / 2 | Creating a backup | 969..1397 | 78 | 0.6711 | 0.3289 | False |

## paragraph / service-endpoints

The replicas are ready but the Service has no usable endpoints. What label relationship should I compare?

Answerable: True. Expected: services / Selecting application Pods. First relevant rank: 1; similarity: 0.5696; highest irrelevant similarity: 0.5165.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | services / 1 | Selecting application Pods | 462..963 | 88 | 0.4304 | 0.5696 | True |
| 2 | deployments / 9 | Scaling and routing boundaries | 3833..3992 | 31 | 0.4835 | 0.5165 | False |
| 3 | services / 2 | Selecting application Pods | 965..1412 | 76 | 0.5355 | 0.4645 | False |
| 4 | deployments / 4 | Readiness and liveness checks | 1955..2416 | 85 | 0.5658 | 0.4342 | False |
| 5 | services / 9 | Investigation checklist | 3750..4039 | 53 | 0.5743 | 0.4257 | False |

## paragraph / service-ports

What does targetPort refer to, compared with the port used by Service clients?

Answerable: True. Expected: services / Service ports and target ports. First relevant rank: 1; similarity: 0.5618; highest irrelevant similarity: 0.4700.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | services / 3 | Service ports and target ports | 1414..1877 | 84 | 0.4382 | 0.5618 | True |
| 2 | services / 4 | Service ports and target ports | 1879..2283 | 72 | 0.5300 | 0.4700 | False |
| 3 | services / 9 | Investigation checklist | 3750..4039 | 53 | 0.6240 | 0.3760 | False |
| 4 | deployments / 0 | Kubernetes Deployments | 0..505 | 90 | 0.6570 | 0.3430 | False |
| 5 | services / 5 | ClusterIP and external exposure | 2285..2793 | 88 | 0.6586 | 0.3414 | False |

## paragraph / gateway-host

Which resource matches a web hostname and URL path to a backend Service?

Answerable: True. Expected: gateway / Hostnames and HTTPRoute matches. First relevant rank: 1; similarity: 0.5278; highest irrelevant similarity: 0.4833.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 1 | Hostnames and HTTPRoute matches | 487..967 | 94 | 0.4722 | 0.5278 | True |
| 2 | gateway / 2 | Hostnames and HTTPRoute matches | 969..1382 | 74 | 0.5167 | 0.4833 | False |
| 3 | services / 6 | ClusterIP and external exposure | 2795..3198 | 72 | 0.5680 | 0.4320 | False |
| 4 | gateway / 0 | Gateway API routing | 0..485 | 82 | 0.6025 | 0.3975 | False |
| 5 | deployments / 9 | Scaling and routing boundaries | 3833..3992 | 31 | 0.6118 | 0.3882 | False |

## paragraph / gateway-status

Which HTTPRoute conditions show whether its parent accepts it and its references resolve?

Answerable: True. Expected: gateway / Route status conditions. First relevant rank: 1; similarity: 0.6793; highest irrelevant similarity: 0.5104.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 3 | Route status conditions | 1384..1837 | 80 | 0.3207 | 0.6793 | True |
| 2 | gateway / 1 | Hostnames and HTTPRoute matches | 487..967 | 94 | 0.4896 | 0.5104 | False |
| 3 | gateway / 2 | Hostnames and HTTPRoute matches | 969..1382 | 74 | 0.5522 | 0.4478 | False |
| 4 | gateway / 9 | Routing handover | 3928..4164 | 45 | 0.5839 | 0.4161 | False |
| 5 | gateway / 0 | Gateway API routing | 0..485 | 82 | 0.6144 | 0.3856 | False |

## paragraph / gateway-grant

What permission is needed when a route forwards to a Service in another namespace, and where does it live?

Answerable: True. Expected: gateway / Cross-namespace backend permission. First relevant rank: 1; similarity: 0.6251; highest irrelevant similarity: 0.5607.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 5 | Cross-namespace backend permission | 2249..2709 | 93 | 0.3749 | 0.6251 | True |
| 2 | gateway / 2 | Hostnames and HTTPRoute matches | 969..1382 | 74 | 0.4393 | 0.5607 | False |
| 3 | gateway / 9 | Routing handover | 3928..4164 | 45 | 0.5085 | 0.4915 | False |
| 4 | gateway / 3 | Route status conditions | 1384..1837 | 80 | 0.5480 | 0.4520 | False |
| 5 | gateway / 10 | Routing handover | 4166..4368 | 37 | 0.5706 | 0.4294 | False |

## paragraph / observe-trace

What should I inspect to find where an individual request spent time across several components?

Answerable: True. Expected: observability / Traces across components. First relevant rank: 1; similarity: 0.6399; highest irrelevant similarity: 0.6025.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 5 | Traces across components | 2241..2726 | 83 | 0.3601 | 0.6399 | True |
| 2 | observability / 6 | Traces across components | 2728..3103 | 71 | 0.3975 | 0.6025 | False |
| 3 | observability / 2 | Metrics and aggregate trends | 981..1399 | 70 | 0.5392 | 0.4608 | False |
| 4 | observability / 1 | Metrics and aggregate trends | 501..979 | 82 | 0.6012 | 0.3988 | False |
| 5 | observability / 3 | Logs and request identifiers | 1401..1886 | 93 | 0.6028 | 0.3972 | False |

## paragraph / observe-close

What two recovery checks are required before closing the workshop incident?

Answerable: True. Expected: observability / Alert acknowledgement and resolution. First relevant rank: none; similarity: none; highest irrelevant similarity: 0.6673.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 9 | Alert acknowledgement and resolution | 3727..4064 | 57 | 0.3327 | 0.6673 | False |
| 2 | postgres-backup / 8 | Point-in-time recovery boundary | 3712..3778 | 11 | 0.3643 | 0.6357 | False |
| 3 | postgres-backup / 10 | Point-in-time recovery boundary | 4006..4213 | 36 | 0.3847 | 0.6153 | False |
| 4 | postgres-backup / 4 | Restoring a plain SQL backup | 1837..2301 | 80 | 0.4656 | 0.5344 | False |
| 5 | deployments / 5 | Rollback procedure | 2418..2899 | 85 | 0.4829 | 0.5171 | False |

## paragraph / password-expiry

When does a recovery link expire, and what happens to old links when I request another?

Answerable: True. Expected: password / Link expiry and replacement. First relevant rank: 1; similarity: 0.6324; highest irrelevant similarity: 0.5089.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | password / 3 | Link expiry and replacement | 1423..1873 | 88 | 0.3676 | 0.6324 | True |
| 2 | password / 7 | Temporary lockout | 3054..3469 | 79 | 0.4911 | 0.5089 | False |
| 3 | password / 6 | Sessions after a password change | 2684..3052 | 72 | 0.5504 | 0.4496 | False |
| 4 | password / 5 | Sessions after a password change | 2273..2682 | 77 | 0.5608 | 0.4392 | False |
| 5 | password / 1 | Requesting a reset link | 500..980 | 82 | 0.5785 | 0.4215 | False |

## paragraph / password-session

After recovering my password, do my other devices stay signed in?

Answerable: True. Expected: password / Sessions after a password change. First relevant rank: 3; similarity: 0.4248; highest irrelevant similarity: 0.5662.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | password / 6 | Sessions after a password change | 2684..3052 | 72 | 0.4338 | 0.5662 | False |
| 2 | password / 10 | Lost mailbox access | 3947..4144 | 41 | 0.5637 | 0.4363 | False |
| 3 | password / 5 | Sessions after a password change | 2273..2682 | 77 | 0.5752 | 0.4248 | True |
| 4 | password / 0 | Workshop account recovery | 0..498 | 89 | 0.5913 | 0.4087 | False |
| 5 | password / 4 | Link expiry and replacement | 1875..2271 | 69 | 0.6010 | 0.3990 | False |

## paragraph / solar-inverter

What converts the panels' direct current into the building's alternating current?

Answerable: True. Expected: solar / Panels and the inverter. First relevant rank: 1; similarity: 0.6111; highest irrelevant similarity: 0.4558.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | solar / 3 | Panels and the inverter | 1373..1824 | 85 | 0.3889 | 0.6111 | True |
| 2 | solar / 0 | Workshop solar energy notes | 0..483 | 82 | 0.5442 | 0.4558 | False |
| 3 | solar / 6 | Shade and seasonal output | 2655..3057 | 64 | 0.6500 | 0.3500 | False |
| 4 | solar / 5 | Shade and seasonal output | 2220..2653 | 74 | 0.7379 | 0.2621 | False |
| 5 | solar / 7 | Storage and overnight demand | 3059..3482 | 72 | 0.7509 | 0.2491 | False |

## paragraph / solar-units

How do the display's kilowatts differ from its daily kilowatt-hours?

Answerable: True. Expected: solar / Power and accumulated energy. First relevant rank: 1; similarity: 0.6129; highest irrelevant similarity: 0.5000.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | solar / 1 | Power and accumulated energy | 485..981 | 100 | 0.3871 | 0.6129 | True |
| 2 | solar / 8 | Comparing the display with a bill | 3484..3907 | 75 | 0.5000 | 0.5000 | False |
| 3 | solar / 2 | Power and accumulated energy | 983..1371 | 73 | 0.5630 | 0.4370 | False |
| 4 | solar / 3 | Panels and the inverter | 1373..1824 | 85 | 0.5923 | 0.4077 | False |
| 5 | solar / 6 | Shade and seasonal output | 2655..3057 | 64 | 0.6390 | 0.3610 | False |

## paragraph / bread-proof

Does the final proof happen before or after shaping, compared with bulk fermentation?

Answerable: True. Expected: bread / Shaping and final proof. First relevant rank: 1; similarity: 0.7545; highest irrelevant similarity: 0.5062.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 5 | Shaping and final proof | 2026..2469 | 80 | 0.2455 | 0.7545 | True |
| 2 | bread / 3 | Bulk fermentation | 1257..1678 | 82 | 0.4938 | 0.5062 | False |
| 3 | bread / 4 | Bulk fermentation | 1680..2024 | 62 | 0.5318 | 0.4682 | False |
| 4 | bread / 11 | Cooling before slicing | 4038..4240 | 39 | 0.5948 | 0.4052 | False |
| 5 | bread / 9 | Cooling before slicing | 3612..3981 | 78 | 0.6207 | 0.3793 | False |

## paragraph / bread-cooling

Why might the inside look gummy if I slice a loaf straight from the oven?

Answerable: True. Expected: bread / Cooling before slicing. First relevant rank: 1; similarity: 0.6500; highest irrelevant similarity: 0.5583.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 9 | Cooling before slicing | 3612..3981 | 78 | 0.3500 | 0.6500 | True |
| 2 | bread / 7 | Baking and crust | 2880..3301 | 78 | 0.4417 | 0.5583 | False |
| 3 | bread / 8 | Baking and crust | 3303..3610 | 54 | 0.5064 | 0.4936 | False |
| 4 | bread / 11 | Cooling before slicing | 4038..4240 | 39 | 0.5853 | 0.4147 | False |
| 5 | bread / 6 | Shaping and final proof | 2471..2878 | 69 | 0.5876 | 0.4124 | False |

## paragraph / tomato-transition

How should indoor tomato seedlings be prepared for living outside permanently?

Answerable: True. Expected: tomatoes / Preparing seedlings for outdoor planting. First relevant rank: 1; similarity: 0.6447; highest irrelevant similarity: 0.4375.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | tomatoes / 1 | Preparing seedlings for outdoor planting | 461..923 | 80 | 0.3553 | 0.6447 | True |
| 2 | tomatoes / 0 | Workshop tomato bed | 0..459 | 84 | 0.5625 | 0.4375 | False |
| 3 | tomatoes / 2 | Preparing seedlings for outdoor planting | 925..1321 | 66 | 0.6167 | 0.3833 | False |
| 4 | tomatoes / 6 | Supports and side shoots | 2619..2952 | 65 | 0.6893 | 0.3107 | False |
| 5 | tomatoes / 3 | Watering and moisture consistency | 1323..1786 | 82 | 0.7336 | 0.2664 | False |

## paragraph / tomato-split

What watering pattern could contribute to tomatoes splitting?

Answerable: True. Expected: tomatoes / Watering and moisture consistency. First relevant rank: 1; similarity: 0.5488; highest irrelevant similarity: 0.4421.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | tomatoes / 4 | Watering and moisture consistency | 1788..2179 | 66 | 0.4512 | 0.5488 | True |
| 2 | tomatoes / 8 | Pollination and fruit set | 3388..3709 | 60 | 0.5579 | 0.4421 | False |
| 3 | tomatoes / 0 | Workshop tomato bed | 0..459 | 84 | 0.6153 | 0.3847 | False |
| 4 | tomatoes / 3 | Watering and moisture consistency | 1323..1786 | 82 | 0.6817 | 0.3183 | False |
| 5 | tomatoes / 6 | Supports and side shoots | 2619..2952 | 65 | 0.6829 | 0.3171 | False |

## paragraph / train-seat

On North Valley, is a reserved seat sufficient permission to travel without a ticket?

Answerable: True. Expected: trains / Tickets and seat reservations. First relevant rank: 1; similarity: 0.6147; highest irrelevant similarity: 0.4885.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 1 | Tickets and seat reservations | 485..949 | 85 | 0.3853 | 0.6147 | True |
| 2 | trains / 7 | Assistance booking | 2874..3325 | 74 | 0.5115 | 0.4885 | False |
| 3 | trains / 2 | Tickets and seat reservations | 951..1288 | 65 | 0.5171 | 0.4829 | False |
| 4 | trains / 3 | Flexible and booked-service tickets | 1290..1740 | 78 | 0.6091 | 0.3909 | False |
| 5 | trains / 0 | Workshop group rail journey | 0..483 | 83 | 0.6099 | 0.3901 | False |

## paragraph / train-delay

My arriving train was late and I missed the booked connection. What should I do before taking another departure?

Answerable: True. Expected: trains / Missed connection after a delay. First relevant rank: 1; similarity: 0.6883; highest irrelevant similarity: 0.5913.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 5 | Missed connection after a delay | 2095..2528 | 73 | 0.3117 | 0.6883 | True |
| 2 | trains / 6 | Missed connection after a delay | 2530..2872 | 59 | 0.4087 | 0.5913 | False |
| 3 | trains / 8 | Assistance booking | 3327..3636 | 56 | 0.5307 | 0.4693 | False |
| 4 | trains / 11 | Platform and boarding checks | 4130..4345 | 39 | 0.5861 | 0.4139 | False |
| 5 | trains / 2 | Tickets and seat reservations | 951..1288 | 65 | 0.6246 | 0.3754 | False |

## paragraph / absent-oracle

How do I configure Oracle Data Guard for automatic failover?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.3984.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 10 | Shift handover checklist | 4066..4304 | 47 | 0.6016 | 0.3984 | False |
| 2 | gateway / 9 | Routing handover | 3928..4164 | 45 | 0.6906 | 0.3094 | False |
| 3 | deployments / 10 | Scaling and routing boundaries | 3994..4140 | 23 | 0.7072 | 0.2928 | False |
| 4 | trains / 11 | Platform and boarding checks | 4130..4345 | 39 | 0.7247 | 0.2753 | False |
| 5 | deployments / 4 | Readiness and liveness checks | 1955..2416 | 85 | 0.7297 | 0.2703 | False |

## paragraph / absent-quantum

How does quantum error correction protect a logical qubit?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.2179.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 11 | Shift handover checklist | 4306..4503 | 35 | 0.7821 | 0.2179 | False |
| 2 | trains / 12 | Platform and boarding checks | 4347..4502 | 26 | 0.8149 | 0.1851 | False |
| 3 | observability / 9 | Alert acknowledgement and resolution | 3727..4064 | 57 | 0.8153 | 0.1847 | False |
| 4 | postgres-backup / 9 | Point-in-time recovery boundary | 3780..4004 | 42 | 0.8198 | 0.1802 | False |
| 5 | observability / 4 | Logs and request identifiers | 1888..2239 | 68 | 0.8255 | 0.1745 | False |

## paragraph / absent-sourdough

What feeding ratio and schedule should I use to maintain a sourdough starter?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.2929.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | solar / 9 | Comparing the display with a bill | 3909..3962 | 9 | 0.7071 | 0.2929 | False |
| 2 | bread / 1 | Mixing and hydration | 433..871 | 85 | 0.7173 | 0.2827 | False |
| 3 | bread / 3 | Bulk fermentation | 1257..1678 | 82 | 0.7222 | 0.2778 | False |
| 4 | tomatoes / 10 | Harvest and inspection notes | 4101..4269 | 37 | 0.7347 | 0.2653 | False |
| 5 | tomatoes / 8 | Pollination and fruit set | 3388..3709 | 60 | 0.7365 | 0.2635 | False |

## paragraph / absent-train-price

What is the exact price in pounds of a North Valley flexible day ticket?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.4292.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 3 | Flexible and booked-service tickets | 1290..1740 | 78 | 0.5708 | 0.4292 | False |
| 2 | trains / 7 | Assistance booking | 2874..3325 | 74 | 0.6621 | 0.3379 | False |
| 3 | trains / 1 | Tickets and seat reservations | 485..949 | 85 | 0.6991 | 0.3009 | False |
| 4 | trains / 0 | Workshop group rail journey | 0..483 | 83 | 0.7280 | 0.2720 | False |
| 5 | deployments / 8 | Scaling and routing boundaries | 3780..3831 | 9 | 0.7944 | 0.2056 | False |

## fixed-254-50 / pg-sql

How can I load a previously exported SQL database into the rehearsal database?

Answerable: True. Expected: postgres-backup / Restoring a plain SQL backup. First relevant rank: 1; similarity: 0.5246; highest irrelevant similarity: 0.4536.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 1 | Creating a backup / Restoring a plain SQL backup / Restoring a custom archive | 1099..2415 | 252 | 0.4754 | 0.5246 | True |
| 2 | postgres-backup / 2 | Restoring a plain SQL backup / Restoring a custom archive / Retention and offsite copies / Point-in-time recovery boundary | 2150..3562 | 254 | 0.5464 | 0.4536 | False |
| 3 | postgres-backup / 0 | PostgreSQL backup and restore / Creating a backup | 0..1343 | 254 | 0.5947 | 0.4053 | False |
| 4 | postgres-backup / 3 | Point-in-time recovery boundary | 3277..4213 | 167 | 0.6487 | 0.3513 | False |
| 5 | tomatoes / 2 | Supports and side shoots / Pollination and fruit set | 2333..3698 | 254 | 0.7732 | 0.2268 | False |

## fixed-254-50 / pg-retention

How many daily and monthly database exports does the workshop retain?

Answerable: True. Expected: postgres-backup / Retention and offsite copies. First relevant rank: 1; similarity: 0.5228; highest irrelevant similarity: 0.4528.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 2 | Restoring a plain SQL backup / Restoring a custom archive / Retention and offsite copies / Point-in-time recovery boundary | 2150..3562 | 254 | 0.4772 | 0.5228 | True |
| 2 | postgres-backup / 3 | Point-in-time recovery boundary | 3277..4213 | 167 | 0.5472 | 0.4528 | False |
| 3 | postgres-backup / 0 | PostgreSQL backup and restore / Creating a backup | 0..1343 | 254 | 0.5784 | 0.4216 | False |
| 4 | postgres-backup / 1 | Creating a backup / Restoring a plain SQL backup / Restoring a custom archive | 1099..2415 | 252 | 0.5971 | 0.4029 | False |
| 5 | solar / 2 | Shade and seasonal output / Storage and overnight demand / Comparing the display with a bill | 2229..3755 | 254 | 0.6049 | 0.3951 | False |

## fixed-254-50 / pg-custom

Which restore program should I use for a custom-format database archive?

Answerable: True. Expected: postgres-backup / Restoring a custom archive. First relevant rank: 3; similarity: 0.3932; highest irrelevant similarity: 0.5412.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 1 | Creating a backup / Restoring a plain SQL backup / Restoring a custom archive | 1099..2415 | 252 | 0.4588 | 0.5412 | False |
| 2 | postgres-backup / 0 | PostgreSQL backup and restore / Creating a backup | 0..1343 | 254 | 0.4979 | 0.5021 | False |
| 3 | postgres-backup / 2 | Restoring a plain SQL backup / Restoring a custom archive / Retention and offsite copies / Point-in-time recovery boundary | 2150..3562 | 254 | 0.6068 | 0.3932 | True |
| 4 | postgres-backup / 3 | Point-in-time recovery boundary | 3277..4213 | 167 | 0.6299 | 0.3701 | False |
| 5 | deployments / 2 | Readiness and liveness checks / Rollback procedure / Scaling and routing boundaries | 2233..3673 | 254 | 0.8706 | 0.1294 | False |

## fixed-254-50 / deploy-probes

Which probe removes a Pod from normal traffic, and which one triggers a restart?

Answerable: True. Expected: deployments / Readiness and liveness checks. First relevant rank: 1; similarity: 0.5751; highest irrelevant similarity: 0.4910.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | deployments / 1 | Rolling out a new application image / Readiness and liveness checks / Rollback procedure | 1111..2507 | 254 | 0.4249 | 0.5751 | True |
| 2 | deployments / 2 | Readiness and liveness checks / Rollback procedure / Scaling and routing boundaries | 2233..3673 | 254 | 0.5090 | 0.4910 | False |
| 3 | deployments / 3 | Scaling and routing boundaries | 3388..4140 | 133 | 0.5404 | 0.4596 | False |
| 4 | deployments / 0 | Kubernetes Deployments / Rolling out a new application image | 0..1367 | 254 | 0.5880 | 0.4120 | False |
| 5 | services / 1 | Selecting application Pods / Service ports and target ports / ClusterIP and external exposure | 1152..2579 | 254 | 0.5915 | 0.4085 | False |

## fixed-254-50 / deploy-rollback

How do I revert a failed application release, and will that undo database migrations?

Answerable: True. Expected: deployments / Rollback procedure. First relevant rank: 1; similarity: 0.3899; highest irrelevant similarity: 0.3555.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | deployments / 2 | Readiness and liveness checks / Rollback procedure / Scaling and routing boundaries | 2233..3673 | 254 | 0.6101 | 0.3899 | True |
| 2 | postgres-backup / 1 | Creating a backup / Restoring a plain SQL backup / Restoring a custom archive | 1099..2415 | 252 | 0.6445 | 0.3555 | False |
| 3 | postgres-backup / 2 | Restoring a plain SQL backup / Restoring a custom archive / Retention and offsite copies / Point-in-time recovery boundary | 2150..3562 | 254 | 0.7215 | 0.2785 | False |
| 4 | postgres-backup / 0 | PostgreSQL backup and restore / Creating a backup | 0..1343 | 254 | 0.7370 | 0.2630 | False |
| 5 | postgres-backup / 3 | Point-in-time recovery boundary | 3277..4213 | 167 | 0.7499 | 0.2501 | False |

## fixed-254-50 / service-endpoints

The replicas are ready but the Service has no usable endpoints. What label relationship should I compare?

Answerable: True. Expected: services / Selecting application Pods. First relevant rank: 1; similarity: 0.5301; highest irrelevant similarity: 0.4422.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | services / 0 | Kubernetes Services / Selecting application Pods / Service ports and target ports | 0..1441 | 254 | 0.4699 | 0.5301 | True |
| 2 | services / 1 | Selecting application Pods / Service ports and target ports / ClusterIP and external exposure | 1152..2579 | 254 | 0.5578 | 0.4422 | False |
| 3 | deployments / 3 | Scaling and routing boundaries | 3388..4140 | 133 | 0.5749 | 0.4251 | False |
| 4 | deployments / 0 | Kubernetes Deployments / Rolling out a new application image | 0..1367 | 254 | 0.6020 | 0.3980 | False |
| 5 | services / 3 | Headless discovery / Investigation checklist | 3466..4235 | 136 | 0.6170 | 0.3830 | False |

## fixed-254-50 / service-ports

What does targetPort refer to, compared with the port used by Service clients?

Answerable: True. Expected: services / Service ports and target ports. First relevant rank: 1; similarity: 0.5532; highest irrelevant similarity: 0.3014.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | services / 1 | Selecting application Pods / Service ports and target ports / ClusterIP and external exposure | 1152..2579 | 254 | 0.4468 | 0.5532 | True |
| 2 | deployments / 3 | Scaling and routing boundaries | 3388..4140 | 133 | 0.6986 | 0.3014 | False |
| 3 | services / 2 | ClusterIP and external exposure / Headless discovery / Investigation checklist | 2298..3751 | 254 | 0.6996 | 0.3004 | False |
| 4 | services / 0 | Kubernetes Services / Selecting application Pods / Service ports and target ports | 0..1441 | 254 | 0.7011 | 0.2989 | False |
| 5 | deployments / 0 | Kubernetes Deployments / Rolling out a new application image | 0..1367 | 254 | 0.7651 | 0.2349 | False |

## fixed-254-50 / gateway-host

Which resource matches a web hostname and URL path to a backend Service?

Answerable: True. Expected: gateway / Hostnames and HTTPRoute matches. First relevant rank: 1; similarity: 0.4659; highest irrelevant similarity: 0.4393.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 0 | Gateway API routing / Hostnames and HTTPRoute matches / Route status conditions | 0..1399 | 254 | 0.5341 | 0.4659 | True |
| 2 | gateway / 1 | Hostnames and HTTPRoute matches / Route status conditions / Cross-namespace backend permission | 1125..2580 | 254 | 0.5607 | 0.4393 | False |
| 3 | gateway / 2 | Cross-namespace backend permission / TLS termination boundary | 2310..3698 | 254 | 0.6171 | 0.3829 | False |
| 4 | deployments / 3 | Scaling and routing boundaries | 3388..4140 | 133 | 0.6814 | 0.3186 | False |
| 5 | services / 2 | ClusterIP and external exposure / Headless discovery / Investigation checklist | 2298..3751 | 254 | 0.6915 | 0.3085 | False |

## fixed-254-50 / gateway-status

Which HTTPRoute conditions show whether its parent accepts it and its references resolve?

Answerable: True. Expected: gateway / Route status conditions. First relevant rank: 1; similarity: 0.5020; highest irrelevant similarity: 0.4528.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 1 | Hostnames and HTTPRoute matches / Route status conditions / Cross-namespace backend permission | 1125..2580 | 254 | 0.4980 | 0.5020 | True |
| 2 | gateway / 0 | Gateway API routing / Hostnames and HTTPRoute matches / Route status conditions | 0..1399 | 254 | 0.5472 | 0.4528 | False |
| 3 | gateway / 3 | TLS termination boundary / Routing handover | 3398..4368 | 175 | 0.6702 | 0.3298 | False |
| 4 | gateway / 2 | Cross-namespace backend permission / TLS termination boundary | 2310..3698 | 254 | 0.7246 | 0.2754 | False |
| 5 | observability / 1 | Metrics and aggregate trends / Logs and request identifiers / Traces across components | 1173..2561 | 254 | 0.7429 | 0.2571 | False |

## fixed-254-50 / gateway-grant

What permission is needed when a route forwards to a Service in another namespace, and where does it live?

Answerable: True. Expected: gateway / Cross-namespace backend permission. First relevant rank: 1; similarity: 0.5608; highest irrelevant similarity: 0.4204.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 1 | Hostnames and HTTPRoute matches / Route status conditions / Cross-namespace backend permission | 1125..2580 | 254 | 0.4392 | 0.5608 | True |
| 2 | gateway / 2 | Cross-namespace backend permission / TLS termination boundary | 2310..3698 | 254 | 0.4827 | 0.5173 | True |
| 3 | gateway / 3 | TLS termination boundary / Routing handover | 3398..4368 | 175 | 0.5796 | 0.4204 | False |
| 4 | gateway / 0 | Gateway API routing / Hostnames and HTTPRoute matches / Route status conditions | 0..1399 | 254 | 0.5816 | 0.4184 | False |
| 5 | services / 2 | ClusterIP and external exposure / Headless discovery / Investigation checklist | 2298..3751 | 254 | 0.6273 | 0.3727 | False |

## fixed-254-50 / observe-trace

What should I inspect to find where an individual request spent time across several components?

Answerable: True. Expected: observability / Traces across components. First relevant rank: 1; similarity: 0.6237; highest irrelevant similarity: 0.4994.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 1 | Metrics and aggregate trends / Logs and request identifiers / Traces across components | 1173..2561 | 254 | 0.3763 | 0.6237 | True |
| 2 | observability / 2 | Traces across components / Alert acknowledgement and resolution | 2258..3717 | 253 | 0.4508 | 0.5492 | True |
| 3 | observability / 0 | Observability shift runbook / Metrics and aggregate trends / Logs and request identifiers | 0..1449 | 254 | 0.5006 | 0.4994 | False |
| 4 | solar / 3 | Storage and overnight demand / Comparing the display with a bill | 3469..4274 | 138 | 0.7353 | 0.2647 | False |
| 5 | gateway / 0 | Gateway API routing / Hostnames and HTTPRoute matches / Route status conditions | 0..1399 | 254 | 0.7517 | 0.2483 | False |

## fixed-254-50 / observe-close

What two recovery checks are required before closing the workshop incident?

Answerable: True. Expected: observability / Alert acknowledgement and resolution. First relevant rank: 1; similarity: 0.6648; highest irrelevant similarity: 0.5641.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 3 | Alert acknowledgement and resolution / Shift handover checklist | 3406..4503 | 191 | 0.3352 | 0.6648 | True |
| 2 | postgres-backup / 3 | Point-in-time recovery boundary | 3277..4213 | 167 | 0.4359 | 0.5641 | False |
| 3 | password / 2 | Link expiry and replacement / Sessions after a password change / Temporary lockout / Lost mailbox access | 2270..3598 | 254 | 0.4548 | 0.5452 | False |
| 4 | postgres-backup / 2 | Restoring a plain SQL backup / Restoring a custom archive / Retention and offsite copies / Point-in-time recovery boundary | 2150..3562 | 254 | 0.5179 | 0.4821 | False |
| 5 | password / 3 | Temporary lockout / Lost mailbox access | 3329..4144 | 153 | 0.5230 | 0.4770 | False |

## fixed-254-50 / password-expiry

When does a recovery link expire, and what happens to old links when I request another?

Answerable: True. Expected: password / Link expiry and replacement. First relevant rank: 2; similarity: 0.4809; highest irrelevant similarity: 0.4856.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | password / 2 | Link expiry and replacement / Sessions after a password change / Temporary lockout / Lost mailbox access | 2270..3598 | 254 | 0.5144 | 0.4856 | False |
| 2 | password / 1 | Requesting a reset link / Link expiry and replacement / Sessions after a password change | 1162..2538 | 254 | 0.5191 | 0.4809 | True |
| 3 | password / 3 | Temporary lockout / Lost mailbox access | 3329..4144 | 153 | 0.6010 | 0.3990 | False |
| 4 | postgres-backup / 3 | Point-in-time recovery boundary | 3277..4213 | 167 | 0.6168 | 0.3832 | False |
| 5 | password / 0 | Workshop account recovery / Requesting a reset link / Link expiry and replacement | 0..1425 | 254 | 0.6812 | 0.3188 | False |

## fixed-254-50 / password-session

After recovering my password, do my other devices stay signed in?

Answerable: True. Expected: password / Sessions after a password change. First relevant rank: 1; similarity: 0.3993; highest irrelevant similarity: 0.3430.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | password / 2 | Link expiry and replacement / Sessions after a password change / Temporary lockout / Lost mailbox access | 2270..3598 | 254 | 0.6007 | 0.3993 | True |
| 2 | password / 1 | Requesting a reset link / Link expiry and replacement / Sessions after a password change | 1162..2538 | 254 | 0.6548 | 0.3452 | True |
| 3 | password / 0 | Workshop account recovery / Requesting a reset link / Link expiry and replacement | 0..1425 | 254 | 0.6570 | 0.3430 | False |
| 4 | password / 3 | Temporary lockout / Lost mailbox access | 3329..4144 | 153 | 0.7264 | 0.2736 | False |
| 5 | trains / 3 | Assistance booking / Platform and boarding checks | 3494..4502 | 175 | 0.8627 | 0.1373 | False |

## fixed-254-50 / solar-inverter

What converts the panels' direct current into the building's alternating current?

Answerable: True. Expected: solar / Panels and the inverter. First relevant rank: 1; similarity: 0.3696; highest irrelevant similarity: 0.3452.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | solar / 1 | Power and accumulated energy / Panels and the inverter / Shade and seasonal output | 1095..2525 | 254 | 0.6304 | 0.3696 | True |
| 2 | solar / 2 | Shade and seasonal output / Storage and overnight demand / Comparing the display with a bill | 2229..3755 | 254 | 0.6548 | 0.3452 | False |
| 3 | solar / 0 | Workshop solar energy notes / Power and accumulated energy | 0..1363 | 253 | 0.7152 | 0.2848 | False |
| 4 | solar / 3 | Storage and overnight demand / Comparing the display with a bill | 3469..4274 | 138 | 0.7638 | 0.2362 | False |
| 5 | tomatoes / 0 | Workshop tomato bed / Preparing seedlings for outdoor planting / Watering and moisture consistency | 0..1469 | 254 | 0.8576 | 0.1424 | False |

## fixed-254-50 / solar-units

How do the display's kilowatts differ from its daily kilowatt-hours?

Answerable: True. Expected: solar / Power and accumulated energy. First relevant rank: 1; similarity: 0.5362; highest irrelevant similarity: 0.4266.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | solar / 0 | Workshop solar energy notes / Power and accumulated energy | 0..1363 | 253 | 0.4638 | 0.5362 | True |
| 2 | solar / 2 | Shade and seasonal output / Storage and overnight demand / Comparing the display with a bill | 2229..3755 | 254 | 0.5734 | 0.4266 | False |
| 3 | solar / 1 | Power and accumulated energy / Panels and the inverter / Shade and seasonal output | 1095..2525 | 254 | 0.5876 | 0.4124 | False |
| 4 | solar / 3 | Storage and overnight demand / Comparing the display with a bill | 3469..4274 | 138 | 0.6460 | 0.3540 | False |
| 5 | observability / 0 | Observability shift runbook / Metrics and aggregate trends / Logs and request identifiers | 0..1449 | 254 | 0.7931 | 0.2069 | False |

## fixed-254-50 / bread-proof

Does the final proof happen before or after shaping, compared with bulk fermentation?

Answerable: True. Expected: bread / Shaping and final proof. First relevant rank: 1; similarity: 0.6032; highest irrelevant similarity: 0.5044.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 1 | Mixing and hydration / Bulk fermentation / Shaping and final proof | 1095..2474 | 254 | 0.3968 | 0.6032 | True |
| 2 | bread / 2 | Shaping and final proof / Baking and crust / Cooling before slicing | 2184..3629 | 254 | 0.4956 | 0.5044 | False |
| 3 | bread / 3 | Baking and crust / Cooling before slicing | 3353..4240 | 173 | 0.6075 | 0.3925 | False |
| 4 | bread / 0 | Workshop bread session / Mixing and hydration / Bulk fermentation | 0..1351 | 253 | 0.6218 | 0.3782 | False |
| 5 | tomatoes / 1 | Preparing seedlings for outdoor planting / Watering and moisture consistency / Supports and side shoots | 1159..2605 | 253 | 0.7624 | 0.2376 | False |

## fixed-254-50 / bread-cooling

Why might the inside look gummy if I slice a loaf straight from the oven?

Answerable: True. Expected: bread / Cooling before slicing. First relevant rank: 1; similarity: 0.6109; highest irrelevant similarity: 0.4697.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 3 | Baking and crust / Cooling before slicing | 3353..4240 | 173 | 0.3891 | 0.6109 | True |
| 2 | bread / 2 | Shaping and final proof / Baking and crust / Cooling before slicing | 2184..3629 | 254 | 0.5303 | 0.4697 | False |
| 3 | bread / 1 | Mixing and hydration / Bulk fermentation / Shaping and final proof | 1095..2474 | 254 | 0.6612 | 0.3388 | False |
| 4 | bread / 0 | Workshop bread session / Mixing and hydration / Bulk fermentation | 0..1351 | 253 | 0.7799 | 0.2201 | False |
| 5 | gateway / 2 | Cross-namespace backend permission / TLS termination boundary | 2310..3698 | 254 | 0.9121 | 0.0879 | False |

## fixed-254-50 / tomato-transition

How should indoor tomato seedlings be prepared for living outside permanently?

Answerable: True. Expected: tomatoes / Preparing seedlings for outdoor planting. First relevant rank: 1; similarity: 0.5950; highest irrelevant similarity: 0.3417.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | tomatoes / 0 | Workshop tomato bed / Preparing seedlings for outdoor planting / Watering and moisture consistency | 0..1469 | 254 | 0.4050 | 0.5950 | True |
| 2 | tomatoes / 2 | Supports and side shoots / Pollination and fruit set | 2333..3698 | 254 | 0.6583 | 0.3417 | False |
| 3 | tomatoes / 1 | Preparing seedlings for outdoor planting / Watering and moisture consistency / Supports and side shoots | 1159..2605 | 253 | 0.7314 | 0.2686 | False |
| 4 | tomatoes / 3 | Pollination and fruit set / Harvest and inspection notes | 3436..4389 | 177 | 0.7572 | 0.2428 | False |
| 5 | solar / 1 | Power and accumulated energy / Panels and the inverter / Shade and seasonal output | 1095..2525 | 254 | 0.7901 | 0.2099 | False |

## fixed-254-50 / tomato-split

What watering pattern could contribute to tomatoes splitting?

Answerable: True. Expected: tomatoes / Watering and moisture consistency. First relevant rank: 1; similarity: 0.4801; highest irrelevant similarity: 0.4400.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | tomatoes / 1 | Preparing seedlings for outdoor planting / Watering and moisture consistency / Supports and side shoots | 1159..2605 | 253 | 0.5199 | 0.4801 | True |
| 2 | tomatoes / 0 | Workshop tomato bed / Preparing seedlings for outdoor planting / Watering and moisture consistency | 0..1469 | 254 | 0.5600 | 0.4400 | False |
| 3 | tomatoes / 2 | Supports and side shoots / Pollination and fruit set | 2333..3698 | 254 | 0.5851 | 0.4149 | False |
| 4 | tomatoes / 3 | Pollination and fruit set / Harvest and inspection notes | 3436..4389 | 177 | 0.6171 | 0.3829 | False |
| 5 | bread / 0 | Workshop bread session / Mixing and hydration / Bulk fermentation | 0..1351 | 253 | 0.8123 | 0.1877 | False |

## fixed-254-50 / train-seat

On North Valley, is a reserved seat sufficient permission to travel without a ticket?

Answerable: True. Expected: trains / Tickets and seat reservations. First relevant rank: 1; similarity: 0.5994; highest irrelevant similarity: 0.4818.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 0 | Workshop group rail journey / Tickets and seat reservations / Flexible and booked-service tickets | 0..1410 | 254 | 0.4006 | 0.5994 | True |
| 2 | trains / 1 | Tickets and seat reservations / Flexible and booked-service tickets / Missed connection after a delay | 1126..2594 | 254 | 0.5182 | 0.4818 | False |
| 3 | trains / 2 | Missed connection after a delay / Assistance booking / Platform and boarding checks | 2290..3780 | 254 | 0.5527 | 0.4473 | False |
| 4 | trains / 3 | Assistance booking / Platform and boarding checks | 3494..4502 | 175 | 0.7217 | 0.2783 | False |
| 5 | gateway / 0 | Gateway API routing / Hostnames and HTTPRoute matches / Route status conditions | 0..1399 | 254 | 0.7422 | 0.2578 | False |

## fixed-254-50 / train-delay

My arriving train was late and I missed the booked connection. What should I do before taking another departure?

Answerable: True. Expected: trains / Missed connection after a delay. First relevant rank: 2; similarity: 0.4269; highest irrelevant similarity: 0.4284.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 2 | Missed connection after a delay / Assistance booking / Platform and boarding checks | 2290..3780 | 254 | 0.5716 | 0.4284 | False |
| 2 | trains / 1 | Tickets and seat reservations / Flexible and booked-service tickets / Missed connection after a delay | 1126..2594 | 254 | 0.5731 | 0.4269 | True |
| 3 | trains / 3 | Assistance booking / Platform and boarding checks | 3494..4502 | 175 | 0.5886 | 0.4114 | False |
| 4 | trains / 0 | Workshop group rail journey / Tickets and seat reservations / Flexible and booked-service tickets | 0..1410 | 254 | 0.7101 | 0.2899 | False |
| 5 | services / 3 | Headless discovery / Investigation checklist | 3466..4235 | 136 | 0.7130 | 0.2870 | False |

## fixed-254-50 / absent-oracle

How do I configure Oracle Data Guard for automatic failover?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.3233.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 3 | Alert acknowledgement and resolution / Shift handover checklist | 3406..4503 | 191 | 0.6767 | 0.3233 | False |
| 2 | observability / 0 | Observability shift runbook / Metrics and aggregate trends / Logs and request identifiers | 0..1449 | 254 | 0.7610 | 0.2390 | False |
| 3 | deployments / 3 | Scaling and routing boundaries | 3388..4140 | 133 | 0.7626 | 0.2374 | False |
| 4 | deployments / 1 | Rolling out a new application image / Readiness and liveness checks / Rollback procedure | 1111..2507 | 254 | 0.7627 | 0.2373 | False |
| 5 | gateway / 3 | TLS termination boundary / Routing handover | 3398..4368 | 175 | 0.7640 | 0.2360 | False |

## fixed-254-50 / absent-quantum

How does quantum error correction protect a logical qubit?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.1723.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 3 | Alert acknowledgement and resolution / Shift handover checklist | 3406..4503 | 191 | 0.8277 | 0.1723 | False |
| 2 | gateway / 0 | Gateway API routing / Hostnames and HTTPRoute matches / Route status conditions | 0..1399 | 254 | 0.8766 | 0.1234 | False |
| 3 | deployments / 2 | Readiness and liveness checks / Rollback procedure / Scaling and routing boundaries | 2233..3673 | 254 | 0.8858 | 0.1142 | False |
| 4 | observability / 0 | Observability shift runbook / Metrics and aggregate trends / Logs and request identifiers | 0..1449 | 254 | 0.8911 | 0.1089 | False |
| 5 | password / 3 | Temporary lockout / Lost mailbox access | 3329..4144 | 153 | 0.8961 | 0.1039 | False |

## fixed-254-50 / absent-sourdough

What feeding ratio and schedule should I use to maintain a sourdough starter?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.3290.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 0 | Workshop bread session / Mixing and hydration / Bulk fermentation | 0..1351 | 253 | 0.6710 | 0.3290 | False |
| 2 | tomatoes / 3 | Pollination and fruit set / Harvest and inspection notes | 3436..4389 | 177 | 0.7172 | 0.2828 | False |
| 3 | bread / 1 | Mixing and hydration / Bulk fermentation / Shaping and final proof | 1095..2474 | 254 | 0.7200 | 0.2800 | False |
| 4 | tomatoes / 1 | Preparing seedlings for outdoor planting / Watering and moisture consistency / Supports and side shoots | 1159..2605 | 253 | 0.7463 | 0.2537 | False |
| 5 | solar / 3 | Storage and overnight demand / Comparing the display with a bill | 3469..4274 | 138 | 0.7628 | 0.2372 | False |

## fixed-254-50 / absent-train-price

What is the exact price in pounds of a North Valley flexible day ticket?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.3674.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 0 | Workshop group rail journey / Tickets and seat reservations / Flexible and booked-service tickets | 0..1410 | 254 | 0.6326 | 0.3674 | False |
| 2 | trains / 1 | Tickets and seat reservations / Flexible and booked-service tickets / Missed connection after a delay | 1126..2594 | 254 | 0.6866 | 0.3134 | False |
| 3 | trains / 2 | Missed connection after a delay / Assistance booking / Platform and boarding checks | 2290..3780 | 254 | 0.7445 | 0.2555 | False |
| 4 | solar / 0 | Workshop solar energy notes / Power and accumulated energy | 0..1363 | 253 | 0.8387 | 0.1613 | False |
| 5 | services / 3 | Headless discovery / Investigation checklist | 3466..4235 | 136 | 0.8485 | 0.1515 | False |

## heading-aware / pg-sql

How can I load a previously exported SQL database into the rehearsal database?

Answerable: True. Expected: postgres-backup / Restoring a plain SQL backup. First relevant rank: 1; similarity: 0.5570; highest irrelevant similarity: 0.4692.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 2 | Restoring a plain SQL backup | 1399..2301 | 167 | 0.4430 | 0.5570 | True |
| 2 | postgres-backup / 1 | Creating a backup | 536..1397 | 164 | 0.5308 | 0.4692 | False |
| 3 | postgres-backup / 3 | Restoring a custom archive | 2303..2765 | 88 | 0.5955 | 0.4045 | False |
| 4 | postgres-backup / 0 | PostgreSQL backup and restore | 0..534 | 99 | 0.6746 | 0.3254 | False |
| 5 | postgres-backup / 4 | Retention and offsite copies | 2767..3239 | 83 | 0.6807 | 0.3193 | False |

## heading-aware / pg-retention

How many daily and monthly database exports does the workshop retain?

Answerable: True. Expected: postgres-backup / Retention and offsite copies. First relevant rank: 1; similarity: 0.5997; highest irrelevant similarity: 0.4392.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 4 | Retention and offsite copies | 2767..3239 | 83 | 0.4003 | 0.5997 | True |
| 2 | postgres-backup / 2 | Restoring a plain SQL backup | 1399..2301 | 167 | 0.5608 | 0.4392 | False |
| 3 | postgres-backup / 1 | Creating a backup | 536..1397 | 164 | 0.5636 | 0.4364 | False |
| 4 | postgres-backup / 5 | Point-in-time recovery boundary | 3241..4213 | 176 | 0.5883 | 0.4117 | False |
| 5 | postgres-backup / 0 | PostgreSQL backup and restore | 0..534 | 99 | 0.6101 | 0.3899 | False |

## heading-aware / pg-custom

Which restore program should I use for a custom-format database archive?

Answerable: True. Expected: postgres-backup / Restoring a custom archive. First relevant rank: 1; similarity: 0.5219; highest irrelevant similarity: 0.4551.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | postgres-backup / 3 | Restoring a custom archive | 2303..2765 | 88 | 0.4781 | 0.5219 | True |
| 2 | postgres-backup / 1 | Creating a backup | 536..1397 | 164 | 0.5449 | 0.4551 | False |
| 3 | postgres-backup / 0 | PostgreSQL backup and restore | 0..534 | 99 | 0.5461 | 0.4539 | False |
| 4 | postgres-backup / 2 | Restoring a plain SQL backup | 1399..2301 | 167 | 0.6265 | 0.3735 | False |
| 5 | postgres-backup / 5 | Point-in-time recovery boundary | 3241..4213 | 176 | 0.6463 | 0.3537 | False |

## heading-aware / deploy-probes

Which probe removes a Pod from normal traffic, and which one triggers a restart?

Answerable: True. Expected: deployments / Readiness and liveness checks. First relevant rank: 1; similarity: 0.6012; highest irrelevant similarity: 0.5204.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | deployments / 2 | Readiness and liveness checks | 1456..2416 | 174 | 0.3988 | 0.6012 | True |
| 2 | services / 5 | Investigation checklist | 3661..4235 | 101 | 0.4796 | 0.5204 | False |
| 3 | services / 1 | Selecting application Pods | 462..1412 | 164 | 0.5539 | 0.4461 | False |
| 4 | deployments / 4 | Scaling and routing boundaries | 3276..4140 | 153 | 0.5630 | 0.4370 | False |
| 5 | services / 3 | ClusterIP and external exposure | 2285..3198 | 160 | 0.5999 | 0.4001 | False |

## heading-aware / deploy-rollback

How do I revert a failed application release, and will that undo database migrations?

Answerable: True. Expected: deployments / Rollback procedure. First relevant rank: 1; similarity: 0.4979; highest irrelevant similarity: 0.3482.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | deployments / 3 | Rollback procedure | 2418..3274 | 151 | 0.5021 | 0.4979 | True |
| 2 | postgres-backup / 2 | Restoring a plain SQL backup | 1399..2301 | 167 | 0.6518 | 0.3482 | False |
| 3 | postgres-backup / 3 | Restoring a custom archive | 2303..2765 | 88 | 0.6672 | 0.3328 | False |
| 4 | observability / 5 | Shift handover checklist | 4066..4503 | 82 | 0.7361 | 0.2639 | False |
| 5 | postgres-backup / 0 | PostgreSQL backup and restore | 0..534 | 99 | 0.7575 | 0.2425 | False |

## heading-aware / service-endpoints

The replicas are ready but the Service has no usable endpoints. What label relationship should I compare?

Answerable: True. Expected: services / Selecting application Pods. First relevant rank: 1; similarity: 0.5720; highest irrelevant similarity: 0.4430.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | services / 1 | Selecting application Pods | 462..1412 | 164 | 0.4280 | 0.5720 | True |
| 2 | deployments / 4 | Scaling and routing boundaries | 3276..4140 | 153 | 0.5570 | 0.4430 | False |
| 3 | services / 2 | Service ports and target ports | 1414..2283 | 156 | 0.5809 | 0.4191 | False |
| 4 | deployments / 0 | Kubernetes Deployments | 0..505 | 90 | 0.5886 | 0.4114 | False |
| 5 | services / 0 | Kubernetes Services | 0..460 | 84 | 0.6080 | 0.3920 | False |

## heading-aware / service-ports

What does targetPort refer to, compared with the port used by Service clients?

Answerable: True. Expected: services / Service ports and target ports. First relevant rank: 1; similarity: 0.5389; highest irrelevant similarity: 0.3430.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | services / 2 | Service ports and target ports | 1414..2283 | 156 | 0.4611 | 0.5389 | True |
| 2 | deployments / 0 | Kubernetes Deployments | 0..505 | 90 | 0.6570 | 0.3430 | False |
| 3 | services / 3 | ClusterIP and external exposure | 2285..3198 | 160 | 0.6611 | 0.3389 | False |
| 4 | services / 1 | Selecting application Pods | 462..1412 | 164 | 0.6779 | 0.3221 | False |
| 5 | services / 0 | Kubernetes Services | 0..460 | 84 | 0.7287 | 0.2713 | False |

## heading-aware / gateway-host

Which resource matches a web hostname and URL path to a backend Service?

Answerable: True. Expected: gateway / Hostnames and HTTPRoute matches. First relevant rank: 1; similarity: 0.5112; highest irrelevant similarity: 0.3975.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 1 | Hostnames and HTTPRoute matches | 487..1382 | 168 | 0.4888 | 0.5112 | True |
| 2 | gateway / 0 | Gateway API routing | 0..485 | 82 | 0.6025 | 0.3975 | False |
| 3 | deployments / 0 | Kubernetes Deployments | 0..505 | 90 | 0.6310 | 0.3690 | False |
| 4 | gateway / 3 | Cross-namespace backend permission | 2249..3116 | 167 | 0.6487 | 0.3513 | False |
| 5 | services / 3 | ClusterIP and external exposure | 2285..3198 | 160 | 0.6705 | 0.3295 | False |

## heading-aware / gateway-status

Which HTTPRoute conditions show whether its parent accepts it and its references resolve?

Answerable: True. Expected: gateway / Route status conditions. First relevant rank: 1; similarity: 0.5476; highest irrelevant similarity: 0.4835.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 2 | Route status conditions | 1384..2247 | 144 | 0.4524 | 0.5476 | True |
| 2 | gateway / 1 | Hostnames and HTTPRoute matches | 487..1382 | 168 | 0.5165 | 0.4835 | False |
| 3 | gateway / 5 | Routing handover | 3928..4368 | 82 | 0.5814 | 0.4186 | False |
| 4 | gateway / 0 | Gateway API routing | 0..485 | 82 | 0.6144 | 0.3856 | False |
| 5 | gateway / 4 | TLS termination boundary | 3118..3926 | 144 | 0.6842 | 0.3158 | False |

## heading-aware / gateway-grant

What permission is needed when a route forwards to a Service in another namespace, and where does it live?

Answerable: True. Expected: gateway / Cross-namespace backend permission. First relevant rank: 1; similarity: 0.5194; highest irrelevant similarity: 0.4964.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | gateway / 3 | Cross-namespace backend permission | 2249..3116 | 167 | 0.4806 | 0.5194 | True |
| 2 | gateway / 5 | Routing handover | 3928..4368 | 82 | 0.5036 | 0.4964 | False |
| 3 | gateway / 1 | Hostnames and HTTPRoute matches | 487..1382 | 168 | 0.5330 | 0.4670 | False |
| 4 | services / 3 | ClusterIP and external exposure | 2285..3198 | 160 | 0.5986 | 0.4014 | False |
| 5 | services / 2 | Service ports and target ports | 1414..2283 | 156 | 0.6069 | 0.3931 | False |

## heading-aware / observe-trace

What should I inspect to find where an individual request spent time across several components?

Answerable: True. Expected: observability / Traces across components. First relevant rank: 1; similarity: 0.6938; highest irrelevant similarity: 0.4579.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 3 | Traces across components | 2241..3103 | 154 | 0.3062 | 0.6938 | True |
| 2 | observability / 1 | Metrics and aggregate trends | 501..1399 | 152 | 0.5421 | 0.4579 | False |
| 3 | observability / 2 | Logs and request identifiers | 1401..2239 | 161 | 0.6117 | 0.3883 | False |
| 4 | observability / 0 | Observability shift runbook | 0..499 | 91 | 0.6351 | 0.3649 | False |
| 5 | gateway / 0 | Gateway API routing | 0..485 | 82 | 0.6981 | 0.3019 | False |

## heading-aware / observe-close

What two recovery checks are required before closing the workshop incident?

Answerable: True. Expected: observability / Alert acknowledgement and resolution. First relevant rank: 1; similarity: 0.5908; highest irrelevant similarity: 0.5655.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 4 | Alert acknowledgement and resolution | 3105..4064 | 162 | 0.4092 | 0.5908 | True |
| 2 | observability / 5 | Shift handover checklist | 4066..4503 | 82 | 0.4345 | 0.5655 | False |
| 3 | postgres-backup / 5 | Point-in-time recovery boundary | 3241..4213 | 176 | 0.4367 | 0.5633 | False |
| 4 | deployments / 3 | Rollback procedure | 2418..3274 | 151 | 0.4746 | 0.5254 | False |
| 5 | password / 4 | Temporary lockout | 3054..3469 | 79 | 0.5039 | 0.4961 | False |

## heading-aware / password-expiry

When does a recovery link expire, and what happens to old links when I request another?

Answerable: True. Expected: password / Link expiry and replacement. First relevant rank: 1; similarity: 0.5296; highest irrelevant similarity: 0.5089.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | password / 2 | Link expiry and replacement | 1423..2271 | 157 | 0.4704 | 0.5296 | True |
| 2 | password / 4 | Temporary lockout | 3054..3469 | 79 | 0.4911 | 0.5089 | False |
| 3 | password / 3 | Sessions after a password change | 2273..3052 | 149 | 0.5394 | 0.4606 | False |
| 4 | postgres-backup / 5 | Point-in-time recovery boundary | 3241..4213 | 176 | 0.6027 | 0.3973 | False |
| 5 | password / 1 | Requesting a reset link | 500..1421 | 163 | 0.6261 | 0.3739 | False |

## heading-aware / password-session

After recovering my password, do my other devices stay signed in?

Answerable: True. Expected: password / Sessions after a password change. First relevant rank: 1; similarity: 0.4742; highest irrelevant similarity: 0.4087.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | password / 3 | Sessions after a password change | 2273..3052 | 149 | 0.5258 | 0.4742 | True |
| 2 | password / 0 | Workshop account recovery | 0..498 | 89 | 0.5913 | 0.4087 | False |
| 3 | password / 2 | Link expiry and replacement | 1423..2271 | 157 | 0.6937 | 0.3063 | False |
| 4 | password / 1 | Requesting a reset link | 500..1421 | 163 | 0.7154 | 0.2846 | False |
| 5 | password / 5 | Lost mailbox access | 3471..4144 | 128 | 0.7175 | 0.2825 | False |

## heading-aware / solar-inverter

What converts the panels' direct current into the building's alternating current?

Answerable: True. Expected: solar / Panels and the inverter. First relevant rank: 1; similarity: 0.5301; highest irrelevant similarity: 0.4558.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | solar / 2 | Panels and the inverter | 1373..2218 | 149 | 0.4699 | 0.5301 | True |
| 2 | solar / 0 | Workshop solar energy notes | 0..483 | 82 | 0.5442 | 0.4558 | False |
| 3 | solar / 3 | Shade and seasonal output | 2220..3057 | 138 | 0.6207 | 0.3793 | False |
| 4 | solar / 4 | Storage and overnight demand | 3059..3482 | 72 | 0.7509 | 0.2491 | False |
| 5 | solar / 5 | Comparing the display with a bill | 3484..4274 | 135 | 0.7833 | 0.2167 | False |

## heading-aware / solar-units

How do the display's kilowatts differ from its daily kilowatt-hours?

Answerable: True. Expected: solar / Power and accumulated energy. First relevant rank: 1; similarity: 0.5142; highest irrelevant similarity: 0.4145.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | solar / 1 | Power and accumulated energy | 485..1371 | 173 | 0.4858 | 0.5142 | True |
| 2 | solar / 3 | Shade and seasonal output | 2220..3057 | 138 | 0.5855 | 0.4145 | False |
| 3 | solar / 5 | Comparing the display with a bill | 3484..4274 | 135 | 0.6256 | 0.3744 | False |
| 4 | solar / 2 | Panels and the inverter | 1373..2218 | 149 | 0.6452 | 0.3548 | False |
| 5 | solar / 0 | Workshop solar energy notes | 0..483 | 82 | 0.6959 | 0.3041 | False |

## heading-aware / bread-proof

Does the final proof happen before or after shaping, compared with bulk fermentation?

Answerable: True. Expected: bread / Shaping and final proof. First relevant rank: 1; similarity: 0.7099; highest irrelevant similarity: 0.5328.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 3 | Shaping and final proof | 2026..2878 | 149 | 0.2901 | 0.7099 | True |
| 2 | bread / 2 | Bulk fermentation | 1257..2024 | 144 | 0.4672 | 0.5328 | False |
| 3 | bread / 5 | Cooling before slicing | 3612..4240 | 127 | 0.6084 | 0.3916 | False |
| 4 | bread / 1 | Mixing and hydration | 433..1255 | 149 | 0.6720 | 0.3280 | False |
| 5 | bread / 4 | Baking and crust | 2880..3610 | 132 | 0.7287 | 0.2713 | False |

## heading-aware / bread-cooling

Why might the inside look gummy if I slice a loaf straight from the oven?

Answerable: True. Expected: bread / Cooling before slicing. First relevant rank: 1; similarity: 0.6535; highest irrelevant similarity: 0.5390.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 5 | Cooling before slicing | 3612..4240 | 127 | 0.3465 | 0.6535 | True |
| 2 | bread / 4 | Baking and crust | 2880..3610 | 132 | 0.4610 | 0.5390 | False |
| 3 | bread / 3 | Shaping and final proof | 2026..2878 | 149 | 0.5769 | 0.4231 | False |
| 4 | bread / 2 | Bulk fermentation | 1257..2024 | 144 | 0.7384 | 0.2616 | False |
| 5 | bread / 0 | Workshop bread session | 0..431 | 83 | 0.7714 | 0.2286 | False |

## heading-aware / tomato-transition

How should indoor tomato seedlings be prepared for living outside permanently?

Answerable: True. Expected: tomatoes / Preparing seedlings for outdoor planting. First relevant rank: 1; similarity: 0.5762; highest irrelevant similarity: 0.4375.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | tomatoes / 1 | Preparing seedlings for outdoor planting | 461..1321 | 146 | 0.4238 | 0.5762 | True |
| 2 | tomatoes / 0 | Workshop tomato bed | 0..459 | 84 | 0.5625 | 0.4375 | False |
| 3 | tomatoes / 3 | Supports and side shoots | 2181..2952 | 146 | 0.6637 | 0.3363 | False |
| 4 | tomatoes / 5 | Harvest and inspection notes | 3711..4389 | 125 | 0.7553 | 0.2447 | False |
| 5 | tomatoes / 2 | Watering and moisture consistency | 1323..2179 | 148 | 0.7650 | 0.2350 | False |

## heading-aware / tomato-split

What watering pattern could contribute to tomatoes splitting?

Answerable: True. Expected: tomatoes / Watering and moisture consistency. First relevant rank: 1; similarity: 0.5342; highest irrelevant similarity: 0.3847.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | tomatoes / 2 | Watering and moisture consistency | 1323..2179 | 148 | 0.4658 | 0.5342 | True |
| 2 | tomatoes / 0 | Workshop tomato bed | 0..459 | 84 | 0.6153 | 0.3847 | False |
| 3 | tomatoes / 3 | Supports and side shoots | 2181..2952 | 146 | 0.6179 | 0.3821 | False |
| 4 | tomatoes / 4 | Pollination and fruit set | 2954..3709 | 139 | 0.6233 | 0.3767 | False |
| 5 | tomatoes / 5 | Harvest and inspection notes | 3711..4389 | 125 | 0.6848 | 0.3152 | False |

## heading-aware / train-seat

On North Valley, is a reserved seat sufficient permission to travel without a ticket?

Answerable: True. Expected: trains / Tickets and seat reservations. First relevant rank: 1; similarity: 0.6692; highest irrelevant similarity: 0.4351.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 1 | Tickets and seat reservations | 485..1288 | 150 | 0.3308 | 0.6692 | True |
| 2 | trains / 4 | Assistance booking | 2874..3636 | 130 | 0.5649 | 0.4351 | False |
| 3 | trains / 0 | Workshop group rail journey | 0..483 | 83 | 0.6099 | 0.3901 | False |
| 4 | trains / 3 | Missed connection after a delay | 2095..2872 | 132 | 0.6530 | 0.3470 | False |
| 5 | trains / 2 | Flexible and booked-service tickets | 1290..2093 | 140 | 0.6701 | 0.3299 | False |

## heading-aware / train-delay

My arriving train was late and I missed the booked connection. What should I do before taking another departure?

Answerable: True. Expected: trains / Missed connection after a delay. First relevant rank: 1; similarity: 0.6763; highest irrelevant similarity: 0.3787.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 3 | Missed connection after a delay | 2095..2872 | 132 | 0.3237 | 0.6763 | True |
| 2 | trains / 5 | Platform and boarding checks | 3638..4502 | 152 | 0.6213 | 0.3787 | False |
| 3 | trains / 4 | Assistance booking | 2874..3636 | 130 | 0.6664 | 0.3336 | False |
| 4 | trains / 1 | Tickets and seat reservations | 485..1288 | 150 | 0.6938 | 0.3062 | False |
| 5 | observability / 5 | Shift handover checklist | 4066..4503 | 82 | 0.7247 | 0.2753 | False |

## heading-aware / absent-oracle

How do I configure Oracle Data Guard for automatic failover?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.3638.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 5 | Shift handover checklist | 4066..4503 | 82 | 0.6362 | 0.3638 | False |
| 2 | gateway / 5 | Routing handover | 3928..4368 | 82 | 0.7208 | 0.2792 | False |
| 3 | services / 5 | Investigation checklist | 3661..4235 | 101 | 0.7369 | 0.2631 | False |
| 4 | deployments / 2 | Readiness and liveness checks | 1456..2416 | 174 | 0.7498 | 0.2502 | False |
| 5 | observability / 0 | Observability shift runbook | 0..499 | 91 | 0.7726 | 0.2274 | False |

## heading-aware / absent-quantum

How does quantum error correction protect a logical qubit?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.1356.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | observability / 5 | Shift handover checklist | 4066..4503 | 82 | 0.8644 | 0.1356 | False |
| 2 | trains / 3 | Missed connection after a delay | 2095..2872 | 132 | 0.8658 | 0.1342 | False |
| 3 | trains / 1 | Tickets and seat reservations | 485..1288 | 150 | 0.8660 | 0.1340 | False |
| 4 | password / 2 | Link expiry and replacement | 1423..2271 | 157 | 0.8723 | 0.1277 | False |
| 5 | deployments / 3 | Rollback procedure | 2418..3274 | 151 | 0.8796 | 0.1204 | False |

## heading-aware / absent-sourdough

What feeding ratio and schedule should I use to maintain a sourdough starter?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.2872.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | bread / 1 | Mixing and hydration | 433..1255 | 149 | 0.7128 | 0.2872 | False |
| 2 | bread / 2 | Bulk fermentation | 1257..2024 | 144 | 0.7331 | 0.2669 | False |
| 3 | solar / 5 | Comparing the display with a bill | 3484..4274 | 135 | 0.7634 | 0.2366 | False |
| 4 | tomatoes / 1 | Preparing seedlings for outdoor planting | 461..1321 | 146 | 0.7759 | 0.2241 | False |
| 5 | bread / 0 | Workshop bread session | 0..431 | 83 | 0.7817 | 0.2183 | False |

## heading-aware / absent-train-price

What is the exact price in pounds of a North Valley flexible day ticket?

Answerable: False. Expected:  / . First relevant rank: none; similarity: none; highest irrelevant similarity: 0.3640.

| Rank | Document / chunk | Heading | Offsets | Tokens | Distance | Similarity | Relevant |
| ---: | --- | --- | --- | ---: | ---: | ---: | --- |
| 1 | trains / 2 | Flexible and booked-service tickets | 1290..2093 | 140 | 0.6360 | 0.3640 | False |
| 2 | trains / 4 | Assistance booking | 2874..3636 | 130 | 0.6947 | 0.3053 | False |
| 3 | trains / 1 | Tickets and seat reservations | 485..1288 | 150 | 0.6969 | 0.3031 | False |
| 4 | trains / 0 | Workshop group rail journey | 0..483 | 83 | 0.7280 | 0.2720 | False |
| 5 | password / 0 | Workshop account recovery | 0..498 | 89 | 0.8305 | 0.1695 | False |
